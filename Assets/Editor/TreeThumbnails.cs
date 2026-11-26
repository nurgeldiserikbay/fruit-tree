#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Renders a picture of every tree species, on a transparent background, for
    /// the tree list. Uses the real game models; run again after changing trees.
    /// </summary>
    public static class TreeThumbnails
    {
        public const string Folder = "Assets/Resources/TreeThumbs";
        private const int Size = 512;

        [MenuItem("Fruit Tree/Render Tree Thumbnails")]
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Folder);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.62f, .64f, .6f);
            Shader.SetGlobalVector(EnvironmentController.WeatherDimId, Vector4.zero);

            var sun = new GameObject("Thumbnail Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(1f, .97f, .9f);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var view = new GameObject("Thumbnail Camera").AddComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0f, 0f, 0f, 0f);
            view.fieldOfView = 30f;
            view.nearClipPlane = .1f;
            view.farClipPlane = 200f;

            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            try
            {
                var levels = OrchardGameController.LevelDefinitions;
                for (int i = 0; i < levels.Count; i++)
                {
                    var probe = new GameObject("Thumbnail " + levels[i].SpeciesId);
                    try
                    {
                        var tree = probe.AddComponent<TreeGenerator>();
                        tree.SetAutomaticGeneration(false);
                        tree.Configure(levels[i].SpeciesId, levels[i].FruitCount, 122 + i * 197);
                        Frame(view, probe);
                        view.targetTexture = target;
                        view.Render();
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                        image.Apply();
                        File.WriteAllBytes(Path.Combine(Folder, levels[i].SpeciesId + ".png"), image.EncodeToPNG());
                    }
                    finally { Object.DestroyImmediate(probe); }
                }
            }
            finally
            {
                view.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
            AssetDatabase.Refresh();
            Debug.Log("Fruit Tree: tree thumbnails rendered to " + Folder);
        }

        /// <summary>Fits the whole tree, seen slightly from above, into a square picture.</summary>
        private static void Frame(Camera view, GameObject tree)
        {
            var renderers = tree.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer part in renderers) bounds.Encapsulate(part.bounds);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            float distance = radius / Mathf.Sin(view.fieldOfView * .5f * Mathf.Deg2Rad) * 1.02f;
            Vector3 direction = new Vector3(.25f, .22f, -1f).normalized;
            view.transform.position = bounds.center + direction * distance;
            view.transform.LookAt(bounds.center);
        }
    }
}
#endif
