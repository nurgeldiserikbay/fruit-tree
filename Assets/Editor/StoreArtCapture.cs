#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Renders clean, high-resolution scenes for the Google Play listing into
    /// Builds/store-art/raw: portrait shots at 2160x3840 (downscaled later to
    /// 1080x1920 for smooth edges), a landscape shot for the feature graphic and
    /// the real home screen. Captions are added outside Unity.
    /// Run: Unity -batchmode -projectPath . -executeMethod FruitTree.Editor.StoreArtCapture.Run
    /// (without -nographics and without -quit).
    /// </summary>
    [InitializeOnLoad]
    public static class StoreArtCapture
    {
        private const string ActiveKey = "FruitTree.StoreArtCapture.Active";
        private const string Folder = "Builds/store-art/raw";
        private static int frames;

        private struct Shot
        {
            public string Name, Species;
            public EnvironmentTheme Theme;
            public WeatherKind Weather;
            public float Angle, Fill, Height, Lift;
            public bool Wildlife;
        }

        // Angle: degrees around the tree. Fill: crown width share of the frame.
        // Height: camera height. Lift: how far above the crown centre to aim,
        // as a share of crown height (positive leaves room for a caption on top).
        private static readonly Shot[] Shots =
        {
            new Shot { Name = "01-apple-park", Species = "apple", Theme = EnvironmentTheme.Park,
                Weather = WeatherKind.Clear, Angle = 0f, Fill = .98f, Height = 1.7f, Lift = .2f },
            new Shot { Name = "02-cherry-japanese", Species = "cherry", Theme = EnvironmentTheme.JapaneseGarden,
                Weather = WeatherKind.Clear, Angle = 35f, Fill = .95f, Height = 1.8f, Lift = .2f },
            new Shot { Name = "03-orange-lakeside", Species = "orange", Theme = EnvironmentTheme.Lakeside,
                Weather = WeatherKind.Clear, Angle = -30f, Fill = .95f, Height = 1.6f, Lift = .2f },
            new Shot { Name = "04-plum-rain", Species = "plum", Theme = EnvironmentTheme.Mountains,
                Weather = WeatherKind.Rain, Angle = 60f, Fill = .95f, Height = 1.7f, Lift = .2f },
            new Shot { Name = "05-peach-evening", Species = "peach", Theme = EnvironmentTheme.EveningGarden,
                Weather = WeatherKind.Clear, Angle = -70f, Fill = .95f, Height = 1.7f, Lift = .2f },
            new Shot { Name = "06-lemon-chinese", Species = "lemon", Theme = EnvironmentTheme.ChineseGarden,
                Weather = WeatherKind.Clear, Angle = 110f, Fill = .95f, Height = 1.7f, Lift = .2f },
        };

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static StoreArtCapture()
        {
            if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            if (++frames < 240) return;
            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            int exitCode = 0;
            try
            {
                Directory.CreateDirectory(Folder);
                Camera camera = Camera.main;
                var tree = Object.FindFirstObjectByType<TreeGenerator>();
                var scenery = Object.FindFirstObjectByType<EnvironmentController>();
                var weather = Object.FindFirstObjectByType<WeatherController>();
                var wildlife = Object.FindFirstObjectByType<Wildlife>();

                // The real home screen, with its logo and buttons.
                SaveWithInterface(camera, "00-home.png", 1080, 1920);

                foreach (Shot shot in Shots)
                {
                    Prepare(tree, scenery, weather, shot.Species, shot.Theme, shot.Weather);
                    Frame(camera, tree, shot.Angle, shot.Fill, shot.Height, shot.Lift, 9f / 16f);
                    if (wildlife != null && shot.Wildlife) wildlife.Showcase(camera);
                    Save(camera, shot.Name + ".png", 2160, 3840);
                }

                // Close-up of ripe apples between the leaves.
                Prepare(tree, scenery, weather, "apple", EnvironmentTheme.Park, WeatherKind.Clear);
                Frame(camera, tree, 15f, .98f, 1.7f, .2f, 9f / 16f);
                Vector3 focus = BestFruit(tree, camera.transform.position);
                Vector3 back = (camera.transform.position - focus);
                back.y = 0f;
                camera.transform.position = focus + back.normalized * 1.25f + Vector3.up * .05f;
                camera.transform.LookAt(focus + Vector3.up * .22f);
                Save(camera, "07-apple-closeup.png", 2160, 3840);

                // Wide view for the 1024x500 feature graphic: tree on the right.
                Prepare(tree, scenery, weather, "apple", EnvironmentTheme.Mountains, WeatherKind.Clear);
                Frame(camera, tree, 10f, .5f, 1.8f, .02f, 2048f / 1000f);
                camera.transform.position -= camera.transform.right * TreeBounds(tree).extents.x * .75f;
                Save(camera, "feature-wide.png", 2048, 1000);
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static void Prepare(TreeGenerator tree, EnvironmentController scenery,
            WeatherController weather, string species, EnvironmentTheme theme, WeatherKind kind)
        {
            if (scenery != null) scenery.ApplyThemeDirect(theme);
            if (weather != null) weather.ForceWeather(kind);
            if (tree == null) return;
            var levels = OrchardGameController.LevelDefinitions;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].SpeciesId != species) continue;
                // The same seed the game uses, so the listing shows the real tree.
                tree.Configure(species, levels[i].FruitCount, 122 + i * 197);
                break;
            }
            CrownShadow.RebuildNow();
        }

        private static Bounds TreeBounds(TreeGenerator tree)
        {
            var renderers = tree.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(tree.transform.position + Vector3.up * 3f, Vector3.one);
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>Places the camera so the whole crown fits the frame width.</summary>
        private static void Frame(Camera camera, TreeGenerator tree, float angle, float fill,
            float height, float lift, float aspect)
        {
            Bounds bounds = TreeBounds(tree);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float verticalHalf = camera.fieldOfView * Mathf.Deg2Rad * .5f;
            float horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * aspect);
            float distance = radius / (fill * Mathf.Tan(horizontalHalf)) + radius * .35f;
            // Never so far that the tree gets small in a tall frame.
            // Measured from the trunk, so the near edge of the crown adds a radius.
            float heightFit = bounds.extents.y * 1.15f / Mathf.Tan(verticalHalf) + radius;
            distance = Mathf.Min(distance, heightFit);
            Vector3 centre = bounds.center;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.back;
            Vector3 position = new Vector3(centre.x, 0f, centre.z) + direction * distance;
            position.y = height;
            camera.transform.position = position;
            camera.transform.LookAt(centre + Vector3.up * bounds.extents.y * lift);
        }

        private static Vector3 BestFruit(TreeGenerator tree, Vector3 eye)
        {
            Vector3 best = tree.transform.position + Vector3.up * 3f;
            float bestScore = float.MinValue;
            foreach (Apple fruit in tree.Apples)
            {
                if (fruit == null) continue;
                Vector3 p = fruit.transform.position;
                // Near the camera side, at a comfortable height.
                float score = -Vector3.Distance(p, eye) - Mathf.Abs(p.y - 2.4f) * 1.5f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        private static void SaveWithInterface(Camera camera, string fileName, int width, int height)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + .05f;
            }
            Canvas.ForceUpdateCanvases();
            try { Save(camera, fileName, width, height); }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i].isRootCanvas) canvases[i].renderMode = modes[i];
            }
        }

        private static void Save(Camera camera, string fileName, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Folder, fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
