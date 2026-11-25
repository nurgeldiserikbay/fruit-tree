using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Small life around the tree: a passing flock, butterflies in fair weather and
    /// sparrows hopping on the ground. No colliders, so picking is never blocked.
    /// </summary>
    public sealed class Wildlife : MonoBehaviour
    {
        private const int FlockSize = 5, ButterflyCount = 3, SparrowCount = 3;
        private const float FlockSpeed = 5.5f, FlockReach = 24f;
        private readonly List<Object> owned = new List<Object>();
        private Transform[] birds, butterflies, sparrows;
        private WeatherController weather;
        private bool weatherSearched;
        private System.Random random;

        private int flockCount;
        private float flockTimer, flockTime = -1f;
        private Vector3 flockStart, flockDirection;
        private readonly Vector3[] birdOffsets = new Vector3[FlockSize];
        private readonly float[] birdLags = new float[FlockSize];

        private readonly Vector3[] butterflyTargets = new Vector3[ButterflyCount];
        private readonly Vector3[] butterflyVelocities = new Vector3[ButterflyCount];
        private readonly float[] butterflyRetarget = new float[ButterflyCount];

        private readonly float[] sparrowTimers = new float[SparrowCount];
        private readonly float[] hopTimes = new float[SparrowCount];
        private readonly float[] sparrowHeadings = new float[SparrowCount];
        private readonly Vector3[] hopFrom = new Vector3[SparrowCount];
        private readonly Vector3[] hopTo = new Vector3[SparrowCount];

        // Only the real game scene gets nature, not bare test rigs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var scenery = FindFirstObjectByType<EnvironmentController>();
            if (scenery == null || scenery.skyRenderer == null) return;
            var nature = new GameObject("Nature");
            nature.AddComponent<Wildlife>();
            nature.AddComponent<Brook>().Initialize(scenery);
        }

        private void Start()
        {
            random = new System.Random(System.Environment.TickCount ^ 0x5a17);
            var source = Resources.Load<Material>("WildlifeMaterial");
            if (source == null)
            {
                enabled = false;
                return;
            }
            var material = Track(new Material(source) { name = "Runtime Wildlife" });
            Mesh birdMesh = Track(BuildFlyingBird());
            Mesh sparrowMesh = Track(BuildSparrow());
            birds = new Transform[FlockSize];
            for (int i = 0; i < FlockSize; i++) birds[i] = Spawn("Bird", birdMesh, material);
            Color[] wingColors =
            {
                new Color(.95f, .55f, .16f), new Color(.96f, .95f, .9f), new Color(.95f, .84f, .3f)
            };
            butterflies = new Transform[ButterflyCount];
            for (int i = 0; i < ButterflyCount; i++)
                butterflies[i] = Spawn("Butterfly",
                    Track(BuildButterfly(wingColors[i % wingColors.Length])), material);
            sparrows = new Transform[SparrowCount];
            for (int i = 0; i < SparrowCount; i++)
            {
                sparrows[i] = Spawn("Sparrow", sparrowMesh, material);
                float angle = Between(0f, Mathf.PI * 2f);
                float radius = Between(2.7f, 3.7f);
                PlaceSparrow(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                    Between(0f, 360f));
                sparrowTimers[i] = Between(.5f, 2.5f);
                hopTimes[i] = -1f;
            }
            for (int i = 0; i < ButterflyCount; i++)
            {
                butterflyTargets[i] = AroundTree(3.4f, 5.2f, .6f, 2.3f);
                butterflies[i].position = butterflyTargets[i];
            }
            flockTimer = Between(6f, 14f);
        }

        private Transform Spawn(string label, Mesh mesh, Material material)
        {
            var animal = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            animal.transform.SetParent(transform, false);
            animal.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = animal.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            animal.SetActive(false);
            return animal.transform;
        }

        private T Track<T>(T asset) where T : Object
        {
            owned.Add(asset);
            return asset;
        }

        private float Between(float low, float high)
        {
            return low + (float)random.NextDouble() * (high - low);
        }

        private Vector3 AroundTree(float minRadius, float maxRadius, float minHeight, float maxHeight)
        {
            float angle = Between(0f, Mathf.PI * 2f);
            float radius = Between(minRadius, maxRadius);
            return new Vector3(Mathf.Cos(angle) * radius, Between(minHeight, maxHeight),
                Mathf.Sin(angle) * radius);
        }

        private WeatherKind CurrentWeather()
        {
            if (weather == null && !weatherSearched)
            {
                weather = FindFirstObjectByType<WeatherController>();
                weatherSearched = true;
            }
            return weather != null ? weather.Current : WeatherKind.Clear;
        }

        private void Update()
        {
            if (!GraphicsQuality.High)
            {
                HideAll();
                return;
            }
            float delta = Time.deltaTime;
            WeatherKind kind = CurrentWeather();
            bool dry = kind != WeatherKind.Rain && kind != WeatherKind.Storm;
            UpdateFlock(delta, dry);
            UpdateButterflies(delta, kind == WeatherKind.Clear || kind == WeatherKind.Cloudy);
            UpdateSparrows(delta, dry);
        }

        private void HideAll()
        {
            foreach (Transform[] group in new[] { birds, butterflies, sparrows })
                foreach (Transform animal in group)
                    if (animal.gameObject.activeSelf) animal.gameObject.SetActive(false);
            flockTime = -1f;
        }

        private void UpdateFlock(float delta, bool dry)
        {
            if (flockTime < 0f)
            {
                if (!dry) return;
                flockTimer -= delta;
                if (flockTimer > 0f) return;
                BeginFlock();
            }
            flockTime += delta;
            float duration = FlockReach * 2f / FlockSpeed;
            bool anyFlying = false;
            for (int i = 0; i < FlockSize; i++)
            {
                float t = flockTime - birdLags[i];
                bool flying = i < flockCount && t >= 0f && t <= duration;
                if (birds[i].gameObject.activeSelf != flying) birds[i].gameObject.SetActive(flying);
                if (!flying) continue;
                anyFlying = true;
                Vector3 position = flockStart + flockDirection * (FlockSpeed * t) + birdOffsets[i];
                position.y += Mathf.Sin(t * 1.3f + i) * .25f;
                birds[i].position = position;
                birds[i].rotation = Quaternion.LookRotation(flockDirection) *
                    Quaternion.Euler(0f, 0f, Mathf.Sin(t * .8f + i) * 8f);
            }
            if (!anyFlying && flockTime > 1f)
            {
                flockTime = -1f;
                flockTimer = Between(18f, 45f);
            }
        }

        private void BeginFlock()
        {
            flockTime = 0f;
            flockCount = random.Next(3, FlockSize + 1);
            float angle = Between(0f, Mathf.PI * 2f);
            Vector3 inward = -new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 side = new Vector3(-inward.z, 0f, inward.x);
            flockDirection = (inward + side * Between(-.25f, .25f)).normalized;
            flockStart = -inward * FlockReach + side * Between(-10f, 10f) +
                Vector3.up * Between(8f, 12f);
            Vector3 right = new Vector3(flockDirection.z, 0f, -flockDirection.x);
            for (int i = 0; i < FlockSize; i++)
            {
                // A loose V: alternate sides, each pair a little further back.
                int rank = (i + 1) / 2;
                float sideSign = i % 2 == 0 ? 1f : -1f;
                birdOffsets[i] = right * (sideSign * rank * .9f) - flockDirection * (rank * .8f) +
                    Vector3.up * Between(-.3f, .3f);
                birdLags[i] = Between(0f, .4f);
            }
        }

        private void UpdateButterflies(float delta, bool sunny)
        {
            for (int i = 0; i < ButterflyCount; i++)
            {
                Transform butterfly = butterflies[i];
                if (butterfly.gameObject.activeSelf != sunny)
                {
                    butterfly.gameObject.SetActive(sunny);
                    if (sunny) butterfly.position = AroundTree(3.4f, 5.2f, .6f, 2.3f);
                }
                if (!sunny) continue;
                butterflyRetarget[i] -= delta;
                if (butterflyRetarget[i] <= 0f)
                {
                    butterflyTargets[i] = AroundTree(3.4f, 5.2f, .6f, 2.3f);
                    butterflyRetarget[i] = Between(2f, 4f);
                }
                Vector3 desired = (butterflyTargets[i] - butterfly.position).normalized * .7f;
                butterflyVelocities[i] = Vector3.Lerp(butterflyVelocities[i], desired, delta * 1.5f);
                float time = Time.time;
                Vector3 flutter = new Vector3(Mathf.Sin(time * 7f + i) * .25f,
                    Mathf.Sin(time * 9.3f + i * 2f) * .35f, Mathf.Cos(time * 6f + i) * .25f);
                butterfly.position += (butterflyVelocities[i] + flutter) * delta;
                Vector3 heading = butterflyVelocities[i];
                heading.y = 0f;
                if (heading.sqrMagnitude > .0001f) butterfly.rotation = Quaternion.LookRotation(heading);
            }
        }

        private void UpdateSparrows(float delta, bool dry)
        {
            for (int i = 0; i < SparrowCount; i++)
            {
                Transform sparrow = sparrows[i];
                if (sparrow.gameObject.activeSelf != dry) sparrow.gameObject.SetActive(dry);
                if (!dry) continue;
                if (hopTimes[i] >= 0f)
                {
                    hopTimes[i] += delta;
                    float f = Mathf.Clamp01(hopTimes[i] / .22f);
                    sparrow.position = Vector3.Lerp(hopFrom[i], hopTo[i], f) +
                        Vector3.up * (Mathf.Sin(Mathf.PI * f) * .09f);
                    if (f >= 1f)
                    {
                        hopTimes[i] = -1f;
                        sparrowTimers[i] = Between(.6f, 3f);
                    }
                    continue;
                }
                sparrowTimers[i] -= delta;
                // Short pecks at the ground between hops.
                float peck = Mathf.Repeat(sparrowTimers[i], 1.4f) < .2f ? 38f : 0f;
                sparrow.rotation = Quaternion.Euler(peck, sparrowHeadings[i], 0f);
                if (sparrowTimers[i] > 0f) continue;
                Vector3 from = sparrow.position;
                float heading = sparrowHeadings[i] + Between(-60f, 60f);
                Vector3 step = Quaternion.Euler(0f, heading, 0f) * Vector3.forward * Between(.2f, .45f);
                Vector3 to = from + step;
                float radius = new Vector2(to.x, to.z).magnitude;
                if (radius < 2.4f || radius > 3.9f)
                {
                    // Turn back toward the open ring between the trunk and the brook.
                    Vector3 toward = new Vector3(from.x, 0f, from.z).normalized * 3.1f - new Vector3(from.x, 0f, from.z);
                    heading = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
                    to = from + Quaternion.Euler(0f, heading, 0f) * Vector3.forward * .35f;
                }
                to.y = GroundGenerator.HeightAt(to.x, to.z);
                hopFrom[i] = from;
                hopTo[i] = to;
                hopTimes[i] = 0f;
                sparrowHeadings[i] = heading;
                sparrow.rotation = Quaternion.Euler(0f, heading, 0f);
            }
        }

        private void PlaceSparrow(int index, Vector3 position, float heading)
        {
            position.y = GroundGenerator.HeightAt(position.x, position.z);
            sparrows[index].position = position;
            sparrowHeadings[index] = heading;
            sparrows[index].rotation = Quaternion.Euler(0f, heading, 0f);
        }

        /// <summary>Puts every animal in front of a camera; used for preview screenshots.</summary>
        public void Showcase(Camera view)
        {
            Transform eye = view.transform;
            Vector3 flat = eye.forward;
            flat.y = 0f;
            flat.Normalize();
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);
            for (int i = 0; i < FlockSize; i++)
            {
                birds[i].gameObject.SetActive(true);
                birds[i].position = eye.position + eye.forward * 14f + Vector3.up * 3f +
                    right * ((i - 2) * 1.1f) - flat * (Mathf.Abs(i - 2) * .7f);
                birds[i].rotation = Quaternion.LookRotation(right);
            }
            for (int i = 0; i < ButterflyCount; i++)
            {
                butterflies[i].gameObject.SetActive(true);
                butterflies[i].position = eye.position + eye.forward * 2.2f +
                    right * ((i - 1) * .5f) + Vector3.up * (i * .15f);
                butterflies[i].rotation = Quaternion.LookRotation(right);
            }
            for (int i = 0; i < SparrowCount; i++)
            {
                sparrows[i].gameObject.SetActive(true);
                Vector3 ground = eye.position + flat * (5f + i * .5f) + right * ((i - 1) * .6f);
                PlaceSparrow(i, ground, Mathf.Atan2(right.x, right.z) * Mathf.Rad2Deg + i * 40f);
            }
        }

        private void OnDestroy()
        {
            foreach (Object asset in owned)
                if (asset != null) Destroy(asset);
        }

        // ---- Low-poly meshes. Vertex alpha marks wings; UV2 holds wing speed and travel.

        private sealed class Builder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<Vector2> Flap = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
            private readonly Vector2 flap;

            public Builder(float beatsPerSecond, float tipTravel)
            {
                flap = new Vector2(beatsPerSecond * Mathf.PI * 2f, tipTravel);
            }

            public int Add(Vector3 position, Color color, float wing)
            {
                Vertices.Add(position);
                color.a = wing;
                Colors.Add(color);
                Flap.Add(flap);
                return Vertices.Count - 1;
            }

            public void Tri(int a, int b, int c)
            {
                Triangles.Add(a);
                Triangles.Add(b);
                Triangles.Add(c);
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices);
                mesh.SetColors(Colors);
                mesh.SetUVs(1, Flap);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static Mesh BuildFlyingBird()
        {
            var b = new Builder(2.2f, .16f);
            Color back = new Color(.16f, .15f, .14f), belly = new Color(.30f, .28f, .25f);
            int nose = b.Add(new Vector3(0f, 0f, .15f), back, 0f);
            int top = b.Add(new Vector3(0f, .03f, .02f), back, 0f);
            int bottom = b.Add(new Vector3(0f, -.025f, .02f), belly, 0f);
            int left = b.Add(new Vector3(-.035f, 0f, .02f), back, 0f);
            int right = b.Add(new Vector3(.035f, 0f, .02f), back, 0f);
            int tail = b.Add(new Vector3(0f, 0f, -.13f), back, 0f);
            int tailLeft = b.Add(new Vector3(-.07f, 0f, -.22f), back, 0f);
            int tailRight = b.Add(new Vector3(.07f, 0f, -.22f), back, 0f);
            b.Tri(nose, top, left); b.Tri(nose, right, top);
            b.Tri(nose, left, bottom); b.Tri(nose, bottom, right);
            b.Tri(tail, left, top); b.Tri(tail, top, right);
            b.Tri(tail, bottom, left); b.Tri(tail, right, bottom);
            b.Tri(tail, tailLeft, tailRight);
            for (int side = -1; side <= 1; side += 2)
            {
                int rootFront = b.Add(new Vector3(side * .03f, 0f, .05f), back, 0f);
                int rootBack = b.Add(new Vector3(side * .03f, 0f, -.04f), back, 0f);
                int middle = b.Add(new Vector3(side * .2f, 0f, .02f), back, .6f);
                int trailing = b.Add(new Vector3(side * .22f, 0f, -.07f), back, .6f);
                int tip = b.Add(new Vector3(side * .38f, 0f, -.05f), back, 1f);
                b.Tri(rootFront, middle, rootBack);
                b.Tri(middle, trailing, rootBack);
                b.Tri(middle, tip, trailing);
            }
            return b.Build("Flying Bird");
        }

        public static Mesh BuildButterfly(Color wing)
        {
            var b = new Builder(3.6f, .045f);
            Color edge = wing * .72f, body = new Color(.12f, .1f, .08f);
            int head = b.Add(new Vector3(0f, .004f, .03f), body, 0f);
            int tailEnd = b.Add(new Vector3(0f, .004f, -.04f), body, 0f);
            int under = b.Add(new Vector3(0f, -.004f, 0f), body, 0f);
            b.Tri(head, tailEnd, under);
            for (int side = -1; side <= 1; side += 2)
            {
                int root = b.Add(new Vector3(0f, 0f, .006f), wing, 0f);
                int front = b.Add(new Vector3(side * .055f, 0f, .04f), edge, 1f);
                int outer = b.Add(new Vector3(side * .05f, 0f, -.01f), wing, .9f);
                b.Tri(root, front, outer);
                int lowerRoot = b.Add(new Vector3(0f, 0f, -.006f), wing, 0f);
                int lowerOuter = b.Add(new Vector3(side * .04f, 0f, -.012f), wing, .8f);
                int lowerTip = b.Add(new Vector3(side * .025f, 0f, -.045f), edge, .9f);
                b.Tri(lowerRoot, lowerOuter, lowerTip);
            }
            return b.Build("Butterfly");
        }

        public static Mesh BuildSparrow()
        {
            var b = new Builder(0f, 0f);
            Color back = new Color(.42f, .30f, .19f), belly = new Color(.72f, .66f, .56f);
            Color head = new Color(.45f, .38f, .32f), beak = new Color(.15f, .12f, .1f);
            // Body: a lathe along z, darker on the back and pale underneath.
            float[,] body = { { .075f, 0f }, { .06f, .022f }, { .03f, .034f }, { -.01f, .036f },
                { -.04f, .03f }, { -.06f, .016f }, { -.07f, 0f } };
            Lathe(b, new Vector3(0f, .045f, 0f), body, 8, back, belly);
            float[,] skull = { { .022f, 0f }, { .012f, .019f }, { -.008f, .02f }, { -.02f, 0f } };
            Lathe(b, new Vector3(0f, .072f, .06f), skull, 6, head, head);
            int beakTop = b.Add(new Vector3(0f, .075f, .1f), beak, 0f);
            int beakLeft = b.Add(new Vector3(-.006f, .07f, .08f), beak, 0f);
            int beakRight = b.Add(new Vector3(.006f, .07f, .08f), beak, 0f);
            b.Tri(beakTop, beakLeft, beakRight);
            int tailRoot = b.Add(new Vector3(0f, .05f, -.055f), back, 0f);
            int tailLeft = b.Add(new Vector3(-.014f, .062f, -.115f), back, 0f);
            int tailRight = b.Add(new Vector3(.014f, .062f, -.115f), back, 0f);
            b.Tri(tailRoot, tailLeft, tailRight);
            return b.Build("Sparrow");
        }

        private static void Lathe(Builder b, Vector3 center, float[,] profile, int sides,
            Color upper, Color lower)
        {
            int rings = profile.GetLength(0);
            int first = b.Vertices.Count;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int s = 0; s < sides; s++)
                {
                    float angle = s * Mathf.PI * 2f / sides;
                    float radius = profile[ring, 1];
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * radius * .85f,
                        Mathf.Sin(angle) * radius, profile[ring, 0]);
                    b.Add(center + offset, Color.Lerp(lower, upper, Mathf.Sin(angle) * .5f + .5f), 0f);
                }
            }
            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = first + ring * sides + s;
                    int c = first + ring * sides + (s + 1) % sides;
                    int d = a + sides, e = c + sides;
                    b.Tri(a, d, c);
                    b.Tri(c, d, e);
                }
            }
        }
    }
}
