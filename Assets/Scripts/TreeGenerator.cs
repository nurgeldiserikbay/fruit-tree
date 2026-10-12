using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Seeded orchard tree: one merged wood mesh, one merged leaf mesh, separate pickable fruit.</summary>
    public sealed class TreeGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 122;
        [SerializeField, Range(8, 300)] private int appleCount = 30;
        [SerializeField] private string speciesId = "apple";
        public string SpeciesId => speciesId;
        public bool ReducedMotion { get; private set; }
        [SerializeField, Range(4, 25)] private int leavesPerCluster = 16;
        private readonly List<Apple> apples = new List<Apple>();
        public IReadOnlyList<Apple> Apples => apples;
        public event Action<int, int> HarvestChanged;
        public event Action<Apple> FruitPicked;
        public int HarvestedCount { get; private set; }
        public int TotalApples => appleCount;
        private System.Random random;
        private readonly List<UnityEngine.Object> runtimeResources = new List<UnityEngine.Object>();

        private T Track<T>(T resource) where T : UnityEngine.Object
        {
            runtimeResources.Add(resource);
            return resource;
        }

        private void ReleaseResources()
        {
            foreach (var resource in runtimeResources)
            {
                if (resource == null) continue;
                if (Application.isPlaying) Destroy(resource);
                else DestroyImmediate(resource);
            }
            runtimeResources.Clear();
        }

        private void OnDestroy() { ReleaseResources(); }

        private void Awake()
        {
            ReducedMotion = PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) == 1;
            // The scene intentionally keeps procedural mesh objects out of serialized scene data.
            // Build them reliably on launch so Android and Editor display the same content.
            if (Application.isPlaying && apples.Count == 0) Generate();
        }

        public void SetReducedMotion(bool reduce)
        {
            ReducedMotion = reduce;
            PlayerPrefs.SetInt("FruitTree.ReducedMotion", reduce ? 1 : 0);
            PlayerPrefs.Save();
            Transform leafObject = transform.Find("Merged Leaves");
            if (leafObject == null) return;
            var renderer = leafObject.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null &&
                renderer.sharedMaterial.HasProperty("_Wind"))
                renderer.sharedMaterial.SetFloat("_Wind", reduce ? .008f : .045f);
        }

        public void Configure(string species, int fruitCount, int generationSeed)
        {
            if (species != "apple" && species != "pear" && species != "cherry")
                throw new ArgumentException("Unsupported tree species: " + species);
            if (fruitCount < 1 || fruitCount > 300)
                throw new ArgumentOutOfRangeException(nameof(fruitCount));
            speciesId = species;
            appleCount = fruitCount;
            seed = generationSeed;
            Generate();
        }

        public void RestorePickedFruits(IReadOnlyCollection<string> picked)
        {
            if (picked == null) return;
            var pickedSet = new HashSet<string>(picked, StringComparer.Ordinal);
            HarvestedCount = 0;
            foreach (var fruit in apples)
            {
                if (pickedSet.Contains(fruit.StableFruitId))
                {
                    fruit.RestorePicked();
                    HarvestedCount++;
                }
            }
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        public void Generate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false); // old fruit cannot be tapped during scene switch
                    Destroy(child);
                }
                else DestroyImmediate(child);
            }
            apples.Clear();
            ReleaseResources();
            HarvestedCount = 0;
            if (Application.isPlaying && SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000)
                leavesPerCluster = Mathf.Min(leavesPerCluster, 8);
            random = new System.Random(seed);
            var wood = new ProceduralMesh();
            var foliage = new ProceduralMesh();
            var fruitAnchors = new List<Vector3>();
            var brown = new Color(.86f, .81f, .74f);
            var branchColor = new Color(.9f, .81f, .72f);
            // A slight bend in the trunk and irregularly-spaced limbs avoid the
            // artificial radial symmetry of the first prototype.
            Vector3 trunkBase = Vector3.zero;
            Vector3 trunkLow = new Vector3(.055f, 1.22f, -.025f);
            Vector3 trunkMid = new Vector3(-.04f, 2.53f, .06f);
            Vector3 trunkTop = new Vector3(.04f, 4.02f, -.025f);
            wood.BranchCurve(
                new[] { trunkBase, trunkLow, trunkMid, trunkTop },
                new[] { .43f, .33f, .22f, .035f }, brown);

            const float goldenAngle = 2.39996323f;
            int majorBranches = speciesId == "pear" ? 17 : speciesId == "cherry" ? 23 : 19;
            float crownSpread = speciesId == "pear" ? .79f : speciesId == "cherry" ? .92f : 1f;
            for (int i = 0; i < majorBranches; i++)
            {
                float elevation = i / (float)(majorBranches - 1);
                float height = Mathf.Lerp(1.36f, 3.55f, elevation) + Between(-.13f, .13f);
                float angle = goldenAngle * i + Between(-.18f, .18f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 sideways = new Vector3(-radial.z, 0f, radial.x);
                float length = (2.65f - elevation * 1.2f) * crownSpread * Between(.88f, 1.12f);
                Vector3 origin = new Vector3(Between(-.065f,.065f), height, Between(-.065f,.065f));

                var limbPoints = new Vector3[4];
                var limbRadii = new float[4];
                limbPoints[0] = origin;
                float baseRadius = Mathf.Lerp(.14f, .095f, elevation);
                limbRadii[0] = baseRadius;
                float bend = Between(-.16f, .16f);
                for (int section = 1; section <= 3; section++)
                {
                    float fraction = section / 3f;
                    limbPoints[section] = origin + radial * (length * fraction) +
                        sideways * (Mathf.Sin(fraction * Mathf.PI) * bend) +
                        Vector3.up * (.12f * fraction + .48f * fraction * fraction);
                    limbRadii[section] = Mathf.Lerp(baseRadius, .022f, fraction);
                }
                wood.BranchCurve(limbPoints, limbRadii, branchColor);

                // Each limb carries several smaller twigs with two leaf clusters.
                // All wood/leaves are merged into just two renderer objects.
                for (int twigIndex = 0; twigIndex < 5; twigIndex++)
                {
                    float fraction = .32f + twigIndex * .14f;
                    float sideSign = twigIndex % 2 == 0 ? 1f : -1f;
                    Vector3 start = origin + radial * (length * fraction) +
                        Vector3.up * (.12f * fraction + .48f * fraction * fraction);
                    Vector3 twigDirection = (radial * .65f +
                        sideways * sideSign * Between(.28f, .7f) +
                        Vector3.up * Between(.18f,.58f)).normalized;
                    float twigLength = Between(.44f, .77f) * (1f - elevation * .18f);
                    Vector3 end = start + twigDirection * twigLength;
                    wood.Branch(start, end, .039f, .008f, branchColor);

                    for (int clusterIndex = 0; clusterIndex < 3; clusterIndex++)
                    {
                        float along = .36f + clusterIndex * .29f;
                        Vector3 center = Vector3.Lerp(start, end, along);
                        fruitAnchors.Add(center);
                        for (int leafIndex = 0; leafIndex < leavesPerCluster; leafIndex++)
                        {
                            Vector3 offset = new Vector3(Between(-.39f,.39f),
                                Between(-.31f,.36f), Between(-.39f,.39f));
                            Vector3 direction = (offset +
                                new Vector3(Between(-.3f,.3f), .21f, Between(-.3f,.3f))).normalized;
                            float tint = Between(-.065f,.065f);
                            Color leafColor = new Color(.33f + tint,
                                .59f + Between(-.065f,.09f), .19f + tint);
                            if (speciesId == "pear") leafColor = new Color(.31f+tint, .52f+tint, .18f);
                            if (speciesId == "cherry") leafColor = new Color(.27f+tint, .53f+tint, .18f);
                            foliage.Leaf(center + offset * .75f, direction,
                                speciesId == "cherry" ? Between(.15f,.22f) : Between(.21f,.32f),
                                speciesId == "pear" ? Between(.11f,.16f) : Between(.16f,.22f), leafColor);
                        }
                    }
                }
            }
            // Lightweight crown fillers close large artificial gaps above the
            // highest limbs while keeping most apples readable from outside.
            for (int cluster = 0; cluster < 24; cluster++)
            {
                float heightRatio = (cluster + .5f) / 24f;
                float angle = cluster * goldenAngle;
                float radius = Mathf.Lerp(1.6f, .55f, heightRatio) * Between(.75f, 1.1f);
                Vector3 center = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Lerp(2.65f, 4.25f, heightRatio),
                    Mathf.Sin(angle) * radius);
                for (int leafIndex = 0; leafIndex < 9; leafIndex++)
                {
                    Vector3 offset = new Vector3(
                        Between(-.32f,.32f), Between(-.24f,.23f), Between(-.32f,.32f));
                    Vector3 direction = (offset + new Vector3(
                        Between(-.4f,.4f), .23f, Between(-.4f,.4f))).normalized;
                    float shade = Between(-.055f,.07f);
                    foliage.Leaf(center + offset, direction,
                        Between(.20f,.30f), Between(.15f,.21f),
                        new Color(.34f + shade, .57f + shade, .21f + shade*.5f));
                }
            }
            // Prevent multiple fruits overlapping a single leaf cluster.
            for (int i = fruitAnchors.Count - 1; i > 0; i--)
            {
                int index = random.Next(i + 1);
                Vector3 swap = fruitAnchors[i];
                fruitAnchors[i] = fruitAnchors[index];
                fruitAnchors[index] = swap;
            }
            var barkMaterial = Track(VertexMaterial("Wood", Color.white));
            if (barkMaterial.HasProperty("_MainTex")) barkMaterial.SetTexture("_MainTex", Track(BarkTexture.Create()));
            MakeMeshObject("Merged Wood", Track(wood.Build("Wood")), barkMaterial);
            var leafMaterial = Track(VertexMaterial("Leaves", Color.white));
            if (leafMaterial.HasProperty("_Wind"))
                leafMaterial.SetFloat("_Wind", ReducedMotion ? .008f : .045f);
            if (leafMaterial.HasProperty("_AmbientLight")) leafMaterial.SetFloat("_AmbientLight", .72f);
            if (leafMaterial.HasProperty("_Cull")) leafMaterial.SetFloat("_Cull", 0f);
            MakeMeshObject("Merged Leaves", Track(foliage.Build("Leaves")), leafMaterial);
            Mesh appleMesh = Track(speciesId == "pear" ? ProceduralMesh.Pear() :
                speciesId == "cherry" ? ProceduralMesh.Cherry() : ProceduralMesh.Apple());
            Color fruitColor = speciesId == "pear" ? new Color(.82f, .78f, .23f) :
                speciesId == "cherry" ? new Color(.74f, .045f, .065f) : Color.white;
            Material appleMaterial = Track(LitMaterial("Fruit " + speciesId, fruitColor, .38f));
            if (speciesId == "apple" && appleMaterial.HasProperty("_MainTex"))
                appleMaterial.SetTexture("_MainTex", Track(AppleSkinTexture.Create()));
            Material stemMaterial = Track(LitMaterial("Stem", new Color(.28f,.19f,.095f), .12f));
            for (int i = 0; i < appleCount; i++)
            {
                Vector3 attachment = fruitAnchors[i * fruitAnchors.Count / appleCount];
                Vector3 outward = new Vector3(attachment.x, 0f, attachment.z).normalized;
                // A slight hanging drop gives every fruit a visible attachment to a twig.
                // Keep a few deeper in the crown, and most near its reachable surface.
                Vector3 at = attachment + Vector3.down * Between(.24f, .32f) +
                    outward * (i % 4 == 0 ? .035f : .14f) +
                    new Vector3(Between(-.07f, .07f), 0f, Between(-.07f, .07f));
                var fruit = MakeMeshObject(speciesId + " " + (i + 1), appleMesh, appleMaterial);
                fruit.transform.localPosition = at;
                // More believable apple-to-tree proportions than the first screenshot.
                float fruitScale = speciesId == "cherry" ? Between(.48f, .61f) :
                    speciesId == "pear" ? Between(.88f, 1f) : Between(.79f, .9f);
                fruit.transform.localScale = Vector3.one * fruitScale;
                fruit.transform.localRotation = Quaternion.Euler(Between(-10, 10), Between(0, 360), Between(-10, 10));
                fruit.AddComponent<SphereCollider>().radius =
                    speciesId == "cherry" ? .29f : .25f;
                var component = fruit.AddComponent<Apple>();
                component.Initialize(i);
                component.Harvested += OnHarvest;
                apples.Add(component);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(fruit.transform, false);
                // Attach the end of the stem to the exact twig anchor, rather than
                // letting apples appear to float when viewed from behind.
                Vector3 top = fruit.transform.TransformPoint(new Vector3(0f,
                    speciesId == "pear" ? .25f : .175f, 0f));
                Vector3 twig = transform.TransformPoint(attachment);
                Vector3 axis = twig - top;
                stem.transform.position = (top + twig) * .5f;
                stem.transform.rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
                stem.transform.localScale = new Vector3(.012f, axis.magnitude * .5f, .012f);
                stem.GetComponent<Renderer>().sharedMaterial = stemMaterial;
                if (Application.isPlaying) Destroy(stem.GetComponent<Collider>());
                else DestroyImmediate(stem.GetComponent<Collider>());
            }
            HarvestChanged?.Invoke(0, appleCount);
        }

        /// <summary>
        /// Start a new picking round without recreating meshes or materials.
        /// This avoids garbage collection spikes on Android and keeps the scene stable.
        /// </summary>
        public void ResetHarvest()
        {
            if (apples.Count == 0)
            {
                Generate();
                return;
            }
            foreach (var apple in apples)
                if (apple != null) apple.ResetApple();
            HarvestedCount = 0;
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        private void OnHarvest(Apple apple)
        {
            HarvestedCount++;
            FruitPicked?.Invoke(apple);
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        private GameObject MakeMeshObject(string label, Mesh mesh, Material material)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (label == "Merged Wood")
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            // Avoid dozens of dynamic shadow-casting meshes on constrained phones.
            if (label == "Merged Leaves" || label.StartsWith("apple ") ||
                label.StartsWith("pear ") || label.StartsWith("cherry "))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Material VertexMaterial(string label, Color fallback)
        {
            Shader shader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = label };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallback);
            return mat;
        }

        private static Material LitMaterial(string label, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = label, color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        private float Between(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
