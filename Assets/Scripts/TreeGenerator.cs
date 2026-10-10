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

        public void Generate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            apples.Clear();
            HarvestedCount = 0;
            random = new System.Random(seed);
            var wood = new ProceduralMesh();
            var foliage = new ProceduralMesh();
            var fruitAnchors = new List<Vector3>();
            var brown = new Color(.34f, .23f, .135f);
            var branchColor = new Color(.39f, .26f, .14f);
            wood.Branch(Vector3.zero, new Vector3(0, 1.75f, 0), .39f, .29f, brown);
            wood.Branch(new Vector3(0, 1.75f, 0), new Vector3(0, 3.7f, 0), .29f, .07f, brown);

            for (int ring = 0; ring < 5; ring++)
            {
                int count = ring == 4 ? 4 : 7;
                float h = 1.55f + ring * .43f;
                for (int b = 0; b < count; b++)
                {
                    float angle = (b + ring * .37f) * Mathf.PI * 2f / count + Between(-.16f, .16f);
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 from = new Vector3(0, h, 0);
                    float length = 2.5f - ring * .39f + Between(-.15f, .15f);
                    Vector3 mid = from + direction * length * .55f + Vector3.up * .24f;
                    Vector3 tip = from + direction * length + Vector3.up * (.38f + ring * .12f);
                    wood.Branch(from, mid, .17f - ring * .018f, .10f - ring * .01f, branchColor);
                    wood.Branch(mid, tip, .10f - ring * .01f, .017f, branchColor);
                    for (int cluster = 0; cluster < 3; cluster++)
                    {
                        Vector3 center = Vector3.Lerp(mid, tip, .3f + cluster * .31f);
                        center += new Vector3(Between(-.24f,.24f), Between(-.1f,.3f), Between(-.24f,.24f));
                        var twig = center + new Vector3(Between(-.24f,.24f), Between(.25f,.4f), Between(-.24f,.24f));
                        wood.Branch(center, twig, .032f, .007f, branchColor);
                        fruitAnchors.Add(center + new Vector3(Between(-.18f,.18f), -.15f, Between(-.18f,.18f)));
                        for (int j = 0; j < leavesPerCluster; j++)
                        {
                            Vector3 offset = new Vector3(Between(-.39f,.39f), Between(-.3f,.36f), Between(-.39f,.39f));
                            Vector3 leafDirection = new Vector3(Between(-1,1), Between(-.3f, .9f), Between(-1,1)).normalized;
                            float colorShift = Between(-.07f, .07f);
                            Color leafColor = new Color(.26f + colorShift, .44f + Between(-.06f,.09f), .16f + colorShift);
                            foliage.Leaf(center + offset, leafDirection, Between(.16f,.29f), Between(.11f,.17f), leafColor);
                        }
                    }
                }
            }
            MakeMeshObject("Merged Wood", wood.Build("Wood"), VertexMaterial("Wood", brown));
            MakeMeshObject("Merged Leaves", foliage.Build("Leaves"), VertexMaterial("Leaves", Color.white));
            Mesh appleMesh = ProceduralMesh.Apple();
            Material appleMaterial = LitMaterial("Apple Red", new Color(.74f,.075f,.045f), .45f);
            Material stemMaterial = LitMaterial("Stem", new Color(.28f,.19f,.095f), .12f);
            for (int i = 0; i < appleCount; i++)
            {
                Vector3 at = fruitAnchors[random.Next(fruitAnchors.Count)] + new Vector3(Between(-.12f,.12f), Between(-.12f,.02f), Between(-.12f,.12f));
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
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
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
