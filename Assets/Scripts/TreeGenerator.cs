using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    public sealed class TreeGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 122;
        [SerializeField, Range(8, 50)] private int appleCount = 28;
        private readonly List<Apple> apples = new List<Apple>();
        public IReadOnlyList<Apple> Apples => apples;
        public event Action<int, int> HarvestChanged;
        private int harvested;

        public void Generate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            apples.Clear();
            harvested = 0;
            var rng = new System.Random(seed);
            var bark = Material(new Color(0.31f, 0.19f, 0.105f), 0.2f);
            var leafMaterials = new[] {
                Material(new Color(0.19f, 0.42f, 0.16f), 0.1f),
                Material(new Color(0.27f, 0.52f, 0.19f), 0.1f),
                Material(new Color(0.35f, 0.55f, 0.2f), 0.1f)
            };
            var red = Material(new Color(0.77f, 0.11f, 0.065f), 0.42f);
            var stemMaterial = Material(new Color(0.27f, 0.17f, 0.08f), 0.05f);
            Segment(Vector3.zero, new Vector3(0, 3.5f, 0), 0.37f, bark, "Trunk");
            var anchors = new List<Vector3>();
            for (int ring = 0; ring < 4; ring++)
            {
                float height = 1.75f + ring * 0.43f;
                int count = ring == 3 ? 5 : 7;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + ring * 0.42f) * Mathf.PI * 2 / count;
                    float length = 2.2f - ring * 0.36f + Next(rng, -0.18f, 0.18f);
                    var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    var origin = new Vector3(0, height, 0);
                    var elbow = origin + outward * (length * 0.55f) + Vector3.up * 0.32f;
                    var tip = origin + outward * length + Vector3.up * (0.5f + ring * 0.1f);
                    Segment(origin, elbow, 0.16f, bark, "Branch");
                    Segment(elbow, tip, 0.09f, bark, "Twig");
                    for (int cluster = 0; cluster < 3; cluster++)
                    {
                        var p = Vector3.Lerp(elbow, tip, (cluster + 1) / 3.3f);
                        p += new Vector3(Next(rng, -0.28f, 0.28f), Next(rng, -0.15f, 0.4f), Next(rng, -0.28f, 0.28f));
                        anchors.Add(p);
                        for (int k = 0; k < 13; k++)
                        {
                            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            leaf.name = "Leaf";
                            leaf.transform.SetParent(transform, false);
                            leaf.transform.localPosition = p + InsideSphere(rng) * 0.48f;
                            leaf.transform.localRotation = Quaternion.Euler(Next(rng, 0, 180), Next(rng, 0, 360), Next(rng, 0, 180));
                            leaf.transform.localScale = new Vector3(0.15f, 0.29f, 0.085f);
                            leaf.GetComponent<Renderer>().sharedMaterial = leafMaterials[rng.Next(leafMaterials.Length)];
                            DestroyImmediate(leaf.GetComponent<Collider>());
                        }
                    }
                }
            }
            for (int i = 0; i < appleCount; i++)
            {
                var at = anchors[rng.Next(anchors.Count)] + new Vector3(Next(rng, -0.24f, 0.24f), -0.25f, Next(rng, -0.24f, 0.24f));
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = "Apple_" + i;
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = at;
                obj.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
                obj.GetComponent<Renderer>().sharedMaterial = red;
                var apple = obj.AddComponent<Apple>();
                apple.Harvested += OnHarvest;
                apples.Add(apple);
                Segment(at + Vector3.up * 0.09f, at + Vector3.up * 0.21f, 0.025f, stemMaterial, "AppleStem_" + i, obj.transform);
            }
            HarvestChanged?.Invoke(harvested, appleCount);
        }

        private void OnHarvest(Apple apple)
        {
            harvested++;
            HarvestChanged?.Invoke(harvested, appleCount);
        }

        private void Segment(Vector3 start, Vector3 end, float radius, Material mat, string label, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = label;
            go.transform.SetParent(parent == null ? transform : parent, true);
            go.transform.position = transform.TransformPoint((start + end) * 0.5f);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
            go.transform.localScale = new Vector3(radius, (end - start).magnitude * 0.5f, radius);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Material Material(Color color, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
        private static float Next(System.Random rng, float min, float max) => Mathf.Lerp(min, max, (float)rng.NextDouble());
        private static Vector3 InsideSphere(System.Random rng) => new Vector3(Next(rng, -1, 1), Next(rng, -1, 1), Next(rng, -1, 1)).normalized * Next(rng, 0, 1);
    }
}
