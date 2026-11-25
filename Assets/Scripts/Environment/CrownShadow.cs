using System.Collections;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// A dappled shadow of the crown on the ground. The tree is drawn once from
    /// above into a small texture whenever a tree appears, then softened, so the
    /// shadow has the real gaps between leaves. Fades in overcast weather.
    /// </summary>
    public sealed class CrownShadow : MonoBehaviour
    {
        private const int Resolution = 128, CaptureLayer = 30;
        private const float BaseStrength = .5f;
        private static CrownShadow instance;
        private TreeGenerator tree;
        private OrchardGameController orchard;
        private EnvironmentController scenery;
        private Material material;
        private Mesh mesh;
        private Texture2D shape;
        private MeshRenderer decal;
        private float nextLightCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var owner = FindFirstObjectByType<EnvironmentController>();
            if (owner == null || owner.skyRenderer == null) return;
            new GameObject("Crown Shadow").AddComponent<CrownShadow>();
        }

        private void Start()
        {
            instance = this;
            tree = FindFirstObjectByType<TreeGenerator>();
            orchard = FindFirstObjectByType<OrchardGameController>();
            scenery = FindFirstObjectByType<EnvironmentController>();
            var source = Resources.Load<Material>("CrownShadowMaterial");
            if (tree == null || source == null)
            {
                enabled = false;
                return;
            }
            material = new Material(source) { name = "Runtime Crown Shadow" };
            mesh = new Mesh { name = "Crown Shadow Decal" };
            var part = new GameObject("Shadow Decal", typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(transform, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            decal = part.GetComponent<MeshRenderer>();
            decal.sharedMaterial = material;
            decal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decal.receiveShadows = false;
            if (orchard != null) orchard.TreeDisplayed += RebuildSoon;
            GraphicsQuality.Changed += ApplyQuality;
            ApplyQuality();
            RebuildSoon();
        }

        /// <summary>Redraws the shadow for whatever tree is showing now.</summary>
        public static void RebuildNow()
        {
            if (instance != null && instance.enabled) instance.Rebuild();
        }

        /// <summary>Waits a frame so a newly generated tree has finished replacing the old one.</summary>
        public static void RebuildSoon()
        {
            if (instance != null && instance.enabled) instance.StartCoroutine(instance.RebuildNextFrame());
        }

        private IEnumerator RebuildNextFrame()
        {
            yield return null;
            Rebuild();
        }

        private void ApplyQuality()
        {
            if (decal != null) decal.enabled = GraphicsQuality.High;
        }

        private void Rebuild()
        {
            var parts = tree.GetComponentsInChildren<Renderer>();
            if (parts.Length == 0) return;
            Bounds bounds = parts[0].bounds;
            foreach (Renderer part in parts) bounds.Encapsulate(part.bounds);
            float half = Mathf.Max(bounds.extents.x, bounds.extents.z) + .6f;
            Capture(parts, bounds, half);

            // The sun is high and a little to the side: shift the shadow away from it.
            Vector3 sun = scenery != null && scenery.sun != null ? scenery.sun.transform.forward :
                new Vector3(.45f, -1f, .3f).normalized;
            float lift = bounds.center.y - tree.transform.position.y;
            Vector2 shift = new Vector2(sun.x, sun.z) / Mathf.Max(.35f, -sun.y) * lift * .55f;
            shift = Vector2.ClampMagnitude(shift, 1.8f);
            BuildDecal(new Vector2(bounds.center.x, bounds.center.z) + shift, half);
        }

        private void Capture(Renderer[] parts, Bounds bounds, float half)
        {
            var layers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                layers[i] = parts[i].gameObject.layer;
                parts[i].gameObject.layer = CaptureLayer;
            }
            var eye = new GameObject("Shadow Capture").AddComponent<Camera>();
            RenderTexture target = RenderTexture.GetTemporary(Resolution, Resolution, 16, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                eye.enabled = false;
                eye.orthographic = true;
                eye.orthographicSize = half;
                eye.transform.position = new Vector3(bounds.center.x, bounds.max.y + 2f, bounds.center.z);
                eye.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = new Color(0f, 0f, 0f, 0f);
                eye.cullingMask = 1 << CaptureLayer;
                eye.nearClipPlane = .1f;
                eye.farClipPlane = bounds.size.y + 6f;
                eye.targetTexture = target;
                eye.Render();
                RenderTexture.active = target;
                var read = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                read.Apply();
                Color32[] pixels = read.GetPixels32();
                Destroy(read);
                var alpha = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a / 255f;
                // Soften twice: leaf gaps stay as light dapples, edges become a penumbra.
                alpha = Blur(alpha, Resolution, 2);
                alpha = Blur(alpha, Resolution, 2);
                if (shape == null)
                    shape = new Texture2D(Resolution, Resolution, TextureFormat.Alpha8, false)
                        { name = "Crown Shadow Shape", wrapMode = TextureWrapMode.Clamp };
                var output = new Color32[alpha.Length];
                for (int i = 0; i < alpha.Length; i++)
                    output[i] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(alpha[i]) * 255f));
                shape.SetPixels32(output);
                shape.Apply(false);
                material.mainTexture = shape;
            }
            finally
            {
                RenderTexture.active = previous;
                eye.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Destroy(eye.gameObject);
                for (int i = 0; i < parts.Length; i++)
                    if (parts[i] != null) parts[i].gameObject.layer = layers[i];
            }
        }

        /// <summary>Box blur with the given radius, horizontal then vertical.</summary>
        public static float[] Blur(float[] source, int size, int radius)
        {
            var across = new float[source.Length];
            var result = new float[source.Length];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f; int count = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sx = x + k;
                        if (sx < 0 || sx >= size) continue;
                        sum += source[y * size + sx]; count++;
                    }
                    across[y * size + x] = sum / count;
                }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f; int count = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sy = y + k;
                        if (sy < 0 || sy >= size) continue;
                        sum += across[sy * size + x]; count++;
                    }
                    result[y * size + x] = sum / count;
                }
            return result;
        }

        /// <summary>A grid lying on the bumpy ground, so the shadow never sinks into it.</summary>
        private void BuildDecal(Vector2 centre, float half)
        {
            const int cells = 20;
            var vertices = new Vector3[(cells + 1) * (cells + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[cells * cells * 6];
            for (int z = 0; z <= cells; z++)
                for (int x = 0; x <= cells; x++)
                {
                    float u = x / (float)cells, v = z / (float)cells;
                    float wx = centre.x + (u - .5f) * half * 2f, wz = centre.y + (v - .5f) * half * 2f;
                    int i = z * (cells + 1) + x;
                    vertices[i] = new Vector3(wx, GroundGenerator.HeightAt(wx, wz) + .015f, wz);
                    uv[i] = new Vector2(u, v);
                }
            int t = 0;
            for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    int a = z * (cells + 1) + x, b = a + 1, c = a + cells + 1, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void Update()
        {
            if (scenery == null || scenery.sun == null || Time.unscaledTime < nextLightCheck) return;
            nextLightCheck = Time.unscaledTime + .25f;
            // Strong sun: crisp shadow; overcast or rain: barely there.
            material.SetFloat("_Strength", BaseStrength * Mathf.Clamp01((scenery.sun.intensity - .15f) / .7f));
        }

        private void OnDestroy()
        {
            if (orchard != null) orchard.TreeDisplayed -= RebuildSoon;
            GraphicsQuality.Changed -= ApplyQuality;
            if (instance == this) instance = null;
            if (mesh != null) Destroy(mesh);
            if (shape != null) Destroy(shape);
            if (material != null) Destroy(material);
        }
    }
}
