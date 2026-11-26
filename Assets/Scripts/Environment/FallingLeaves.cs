using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Now and then a leaf lets go and drifts down to the grass, more often in
    /// wind. Uses the tree's own leaf material; six leaves at most, no physics.
    /// </summary>
    public sealed class FallingLeaves : MonoBehaviour
    {
        private const int Pool = 6;
        private const float LeafSize = .13f, RestSeconds = 2.5f;
        private readonly Transform[] leaves = new Transform[Pool];
        private readonly MeshRenderer[] renderers = new MeshRenderer[Pool];
        private readonly float[] age = new float[Pool], phase = new float[Pool], landedAt = new float[Pool];
        private readonly Vector3[] spin = new Vector3[Pool];
        private readonly bool[] falling = new bool[Pool];
        private TreeGenerator tree;
        private Renderer crown;
        private Mesh mesh;
        private System.Random random;
        private float nextLeaf;
        private string species;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var owner = FindFirstObjectByType<EnvironmentController>();
            if (owner == null || owner.skyRenderer == null) return;
            new GameObject("Falling Leaves").AddComponent<FallingLeaves>();
        }

        private void Start()
        {
            random = new System.Random(System.Environment.TickCount ^ 0x1eaf);
            tree = FindFirstObjectByType<TreeGenerator>();
            if (tree == null) { enabled = false; return; }
            mesh = BuildLeafMesh();
            for (int i = 0; i < Pool; i++)
            {
                var leaf = new GameObject("Falling Leaf", typeof(MeshFilter), typeof(MeshRenderer));
                leaf.transform.SetParent(transform, false);
                leaf.GetComponent<MeshFilter>().sharedMesh = mesh;
                renderers[i] = leaf.GetComponent<MeshRenderer>();
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                leaf.SetActive(false);
                leaves[i] = leaf.transform;
            }
            nextLeaf = Between(2f, 5f);
        }

        private float Between(float low, float high) => low + (float)random.NextDouble() * (high - low);

        private static Mesh BuildLeafMesh()
        {
            var leafMesh = new Mesh { name = "Falling Leaf" };
            float h = LeafSize * .5f;
            leafMesh.vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h) };
            leafMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            leafMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            leafMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            leafMesh.RecalculateBounds();
            return leafMesh;
        }

        private void Update()
        {
            if (!GraphicsQuality.High)
            {
                for (int i = 0; i < Pool; i++)
                    if (leaves[i].gameObject.activeSelf) { falling[i] = false; leaves[i].gameObject.SetActive(false); }
                return;
            }
            RefreshMaterial();
            if (crown == null) return;
            float delta = Time.deltaTime;
            float gust = Shader.GetGlobalFloat("_WindGust");
            nextLeaf -= delta * (1f + gust);
            if (nextLeaf <= 0f)
            {
                nextLeaf = Between(3f, 7f);
                Release();
            }
            Vector3 drift = new Vector3(.8f, 0f, .35f).normalized * (.12f + gust * .45f);
            for (int i = 0; i < Pool; i++)
            {
                if (!leaves[i].gameObject.activeSelf) continue;
                Transform leaf = leaves[i];
                if (falling[i])
                {
                    age[i] += delta;
                    float sway = Mathf.Sin(age[i] * 2.4f + phase[i]);
                    Vector3 side = new Vector3(Mathf.Cos(phase[i]), 0f, Mathf.Sin(phase[i]));
                    leaf.position += (Vector3.down * .55f + drift + side * sway * .35f) * delta;
                    leaf.Rotate(spin[i] * delta, Space.Self);
                    float ground = GroundGenerator.HeightAt(leaf.position.x, leaf.position.z) + .02f;
                    if (leaf.position.y <= ground)
                    {
                        falling[i] = false;
                        landedAt[i] = Time.time;
                        leaf.position = new Vector3(leaf.position.x, ground, leaf.position.z);
                        leaf.rotation = Quaternion.Euler(0f, Between(0f, 360f), 0f);
                    }
                }
                else
                {
                    // Rests on the grass, then quietly shrinks away.
                    float rest = Time.time - landedAt[i];
                    if (rest > RestSeconds)
                    {
                        float s = Mathf.Clamp01(1f - (rest - RestSeconds) / .6f);
                        leaf.localScale = Vector3.one * s;
                        if (s <= 0f) leaf.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void Release()
        {
            for (int i = 0; i < Pool; i++)
            {
                if (leaves[i].gameObject.activeSelf) continue;
                Bounds b = crown.bounds;
                Vector3 onShell = Random.onUnitSphere;
                onShell.y = Mathf.Abs(onShell.y) * .5f - .1f;
                Vector3 start = b.center + Vector3.Scale(onShell, b.extents * .85f);
                Transform leaf = leaves[i];
                leaf.position = start;
                leaf.rotation = Random.rotation;
                leaf.localScale = Vector3.one * Between(.8f, 1.15f);
                spin[i] = new Vector3(Between(-140f, 140f), Between(-90f, 90f), Between(-140f, 140f));
                phase[i] = Between(0f, Mathf.PI * 2f);
                age[i] = 0f;
                falling[i] = true;
                leaf.gameObject.SetActive(true);
                return;
            }
        }

        /// <summary>Uses the current tree's leaves: same texture, same colour.</summary>
        private void RefreshMaterial()
        {
            if (crown != null && species == tree.SpeciesId) return;
            Transform merged = tree.transform.Find("Merged Leaves");
            crown = merged != null ? merged.GetComponent<Renderer>() : null;
            if (crown == null) return;
            species = tree.SpeciesId;
            Color tint = FruitSpeciesProfile.IsSupported(species) ?
                FruitSpeciesProfile.Get(species).PhotoLeafTint : Color.white;
            mesh.colors = new[] { tint, tint, tint, tint };
            foreach (MeshRenderer leaf in renderers) leaf.sharedMaterial = crown.sharedMaterial;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
