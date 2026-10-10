#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FruitTree.Editor
{
    public static class FruitTreeSceneBuilder
    {
        [InitializeOnLoadMethod]
        private static void ScheduleFirstScene()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || EditorApplication.isCompiling) return;
                if (!System.IO.File.Exists("Assets/Scenes/Main.unity")) Generate();
            };
        }

        [MenuItem("Fruit Tree/Generate Playable Scene")]
        public static void Generate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Fruit Tree Game");
            app.AddComponent<MobileBootstrap>();
            app.AddComponent<DevelopmentPerformanceMonitor>();

            var root = new GameObject("Apple Tree");
            root.AddComponent<TreeGenerator>();

            EnsureShaderIsInBuild();
            new GameObject("Quiet Meadow").AddComponent<GroundGenerator>();
            CreateSky();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.74f, .87f, .94f);
            camera.fieldOfView = 47f;
            camera.allowHDR = false;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<OrbitPickingCamera>().target = root.transform;

            var sunObject = new GameObject("Warm Sun");
            var light = sunObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.12f;
            light.color = new Color(1f, .95f, .86f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .5f;
            sunObject.transform.rotation = Quaternion.Euler(43f, -32f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.68f, .73f, .77f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogStartDistance = 13f;
            RenderSettings.fogEndDistance = 28f;

            var sound = new GameObject("Orchard Sounds").AddComponent<OrchardAudio>();
            sound.tree = root.GetComponent<TreeGenerator>();
            CreateInterface(root.GetComponent<TreeGenerator>(), sound);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.productName = "Fruit Tree";
            PlayerSettings.companyName = "Fruit Tree Studio";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.nurgeldiserikbay.fruittree");
            // Default Android texture compression is selected from Build Profiles.
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Fruit Tree scene generated! Press Play to build a 3D tree and pick apples.");
        }


        private static void EnsureShaderIsInBuild()
        {
            // Explicit material references prevent shader stripping in Android builds.
            System.IO.Directory.CreateDirectory("Assets/Resources");
            EnsureMaterial("VertexLitReference", "FruitTree/VertexLit");
            EnsureMaterial("SoftSkyReference", "FruitTree/SoftSky");
        }

        private static void EnsureMaterial(string materialName, string shaderName)
        {
            string path = "Assets/Resources/" + materialName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("Unable to find shader: " + shaderName);
                return;
            }
            AssetDatabase.CreateAsset(new Material(shader) { name = materialName }, path);
        }

        private static void CreateSky()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Resources/SoftSkyReference.mat");
            if (material == null) return;
            var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "Soft Sky";
            sky.transform.localScale = Vector3.one * 100f;
            var renderer = sky.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.DestroyImmediate(sky.GetComponent<Collider>());
        }

        private static void CreateInterface(TreeGenerator tree, OrchardAudio sound)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var safe = new GameObject("Safe Area", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            safe.AddComponent<SafeAreaFitter>();

            var panel = new GameObject("Counter Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(safe.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            Anchor(panelRect, new Vector2(.30f, .895f), new Vector2(.70f, .967f));
            panel.GetComponent<Image>().color = new Color(1f, .98f, .93f, .85f);
            var count = MakeLabel("Apple Counter", safe.transform, "0 / 30", 49, new Color(.24f,.31f,.18f),
                new Vector2(.30f, .895f), new Vector2(.70f, .967f));

            var hint = MakeLabel("Control Hint", safe.transform, "Drag to rotate  -  Tap apples", 28,
                new Color(.22f,.30f,.20f), new Vector2(.11f,.07f), new Vector2(.89f,.14f));
            var hud = canvasObject.AddComponent<HarvestHUD>();
            hud.tree = tree; hud.countText = count; hud.messageText = hint;
            // Keep the gameplay clean. Only the small pause button is visible.
            // Restart and sound are accessible through the pause menu.
            var pauseButton = MakeButton("Pause", safe.transform, "II",
                new Vector2(.82f, .89f), new Vector2(.96f, .966f),
                new Color(.98f, .94f, .84f, .91f));

            var menu = new GameObject("Pause Menu", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            menu.transform.SetParent(safe.transform, false);
            Anchor(menu.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            menu.GetComponent<Image>().color = new Color(.12f, .17f, .13f, .76f);
            var pauseController = canvasObject.AddComponent<PauseController>();
            pauseController.panel = menu;
            pauseController.hud = hud;

            MakeLabel("Paused Title", menu.transform, "PAUSED", 70, Color.white,
                new Vector2(.17f, .64f), new Vector2(.83f, .74f));
            var resume = MakeButton("Resume", menu.transform, "CONTINUE",
                new Vector2(.23f, .51f), new Vector2(.77f, .59f),
                new Color(.97f, .97f, .89f, 1f));
            var restart = MakeButton("Restart", menu.transform, "RESTART",
                new Vector2(.23f, .40f), new Vector2(.77f, .48f),
                new Color(.97f, .97f, .89f, 1f));
            var audio = MakeButton("Sound", menu.transform, "SOUND ON",
                new Vector2(.23f, .29f), new Vector2(.77f, .37f),
                new Color(.97f, .97f, .89f, 1f));
            sound.soundStatusText = audio.GetComponentInChildren<Text>();
            UnityEventTools.AddPersistentListener(pauseButton.onClick, pauseController.Toggle);
            UnityEventTools.AddPersistentListener(resume.onClick, pauseController.Resume);
            UnityEventTools.AddPersistentListener(restart.onClick, pauseController.Restart);
            UnityEventTools.AddPersistentListener(audio.onClick, sound.ToggleSound);
            menu.SetActive(false);
        }

        private static Text MakeLabel(string name, Transform parent, string label, int size, Color color,
            Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Anchor(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = size / 2;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Anchor(go.GetComponent<RectTransform>(), min, max);
            go.GetComponent<Image>().color = color;
            var button = go.GetComponent<Button>();
            var tint = button.colors;
            tint.highlightedColor = new Color(.94f, .96f, .88f);
            tint.pressedColor = new Color(.76f, .83f, .71f);
            button.colors = tint;
            MakeLabel(name + " Label", go.transform, text, 25, new Color(.22f,.30f,.19f), Vector2.zero, Vector2.one);
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
