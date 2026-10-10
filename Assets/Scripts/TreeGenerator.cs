using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Seeded orchard tree: one merged wood mesh, one merged leaf mesh, separate pickable fruit.</summary>
    public sealed class TreeGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 122;
        [SerializeField, Range(8, 60)] private int appleCount = 30;
        [SerializeField, Range(4, 25)] private int leavesPerCluster = 13;
        private readonly List<Apple> apples = new List<Apple>();
        public IReadOnlyList<Apple> Apples => apples;
        public event Action<int, int> HarvestChanged;
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
            // The scene intentionally keeps procedural mesh objects out of serialized scene data.
            // Build them reliably on launch so Android and Editor display the same content.
            if (Application.isPlaying && apples.Count == 0) Generate();
        }

        public void Generate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
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
            wood.Branch(trunkBase, trunkLow, .43f, .33f, brown);
            wood.Branch(trunkLow, trunkMid, .33f, .22f, brown);
            wood.Branch(trunkMid, trunkTop, .22f, .035f, brown);

            const float goldenAngle = 2.39996323f;
            const int majorBranches = 19;
            for (int i = 0; i < majorBranches; i++)
            {
                float elevation = i / (float)(majorBranches - 1);
                float height = Mathf.Lerp(1.36f, 3.55f, elevation) + Between(-.13f, .13f);
                float angle = goldenAngle * i + Between(-.18f, .18f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 sideways = new Vector3(-radial.z, 0f, radial.x);
                float length = (2.65f - elevation * 1.2f) * Between(.88f, 1.12f);
                Vector3 origin = new Vector3(Between(-.065f,.065f), height, Between(-.065f,.065f));

                Vector3 previous = origin;
                for (int section = 1; section <= 3; section++)
                {
                    float fraction = section / 3f;
                    Vector3 point = origin + radial * (length * fraction) +
                        sideways * (Mathf.Sin(fraction * Mathf.PI) * Between(-.16f, .16f)) +
                        Vector3.up * (.12f * fraction + .48f * fraction * fraction);
                    float baseRadius = Mathf.Lerp(.14f, .095f, elevation);
                    wood.Branch(previous, point, Mathf.Lerp(baseRadius, .022f, (section - 1) / 3f),
                        Mathf.Lerp(baseRadius, .022f, fraction), branchColor);
                    previous = point;
                }

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

                    for (int clusterIndex = 0; clusterIndex < 2; clusterIndex++)
                    {
                        float along = clusterIndex == 0 ? .57f : .94f;
                        Vector3 center = Vector3.Lerp(start, end, along);
                        fruitAnchors.Add(center + Vector3.down * .1f);
                        for (int leafIndex = 0; leafIndex < leavesPerCluster; leafIndex++)
                        {
                            Vector3 offset = new Vector3(Between(-.39f,.39f),
                                Between(-.31f,.36f), Between(-.39f,.39f));
                            Vector3 direction = (offset +
                                new Vector3(Between(-.3f,.3f), .21f, Between(-.3f,.3f))).normalized;
                            float tint = Between(-.065f,.065f);
                            Color leafColor = new Color(.32f + tint,
                                .56f + Between(-.065f,.095f), .18f + tint);
                            foliage.Leaf(center + offset * .75f, direction,
                                Between(.17f,.29f), Between(.11f,.17f), leafColor);
                        }
                    }
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
            if (leafMaterial.HasProperty("_Wind")) leafMaterial.SetFloat("_Wind", .045f);
            MakeMeshObject("Merged Leaves", Track(foliage.Build("Leaves")), leafMaterial);
            Mesh appleMesh = Track(ProceduralMesh.Apple());
            Material appleMaterial = Track(LitMaterial("Apple Red", Color.white, .38f));
            if (appleMaterial.HasProperty("_MainTex"))
                appleMaterial.SetTexture("_MainTex", Track(AppleSkinTexture.Create()));
            Material stemMaterial = Track(LitMaterial("Stem", new Color(.28f,.19f,.095f), .12f));
            for (int i = 0; i < appleCount; i++)
            {
                Vector3 at = fruitAnchors[i * fruitAnchors.Count / appleCount] + new Vector3(Between(-.12f,.12f), Between(-.12f,.02f), Between(-.12f,.12f));
                // Keep most apples near the outer crown so the picking game stays readable.
                // A smaller group remains deeper in the leaves for exploration.
                Vector3 outward = new Vector3(at.x, 0f, at.z).normalized;
                at += outward * (i % 4 == 0 ? .04f : .18f);
                var fruit = MakeMeshObject("Apple " + (i + 1), appleMesh, appleMaterial);
                fruit.transform.localPosition = at;
                fruit.transform.localRotation = Quaternion.Euler(Between(-10,10), Between(0,360), Between(-10,10));
                fruit.AddComponent<SphereCollider>().radius = .22f;
                var component = fruit.AddComponent<Apple>();
                component.Harvested += OnHarvest;
                apples.Add(component);
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(fruit.transform, false);
                stem.transform.localPosition = new Vector3(0, .20f, 0);
                stem.transform.localScale = new Vector3(.012f, .09f, .012f);
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
            if (label == "Merged Leaves" || label.StartsWith("Apple "))
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
