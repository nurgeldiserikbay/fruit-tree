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
                if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorApplication.delayCall += SchedulePendingScene;
                    return;
                }
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity") == null) Generate();
            };
        }

        private static void SchedulePendingScene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += SchedulePendingScene;
                return;
            }
            if (!System.IO.File.Exists("Assets/Scenes/Main.unity")) Generate();
        }

        [MenuItem("Fruit Tree/Refresh Visuals (Back Up Scene)")]
        public static void RefreshVisuals()
        {
            // Explicit action only: the saved local scene might contain user edits.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string scenePath = "Assets/Scenes/Main.unity";
            if (System.IO.File.Exists(scenePath))
            {
                string backupDirectory = System.IO.Path.Combine(
                    Application.dataPath, "../Builds/SceneBackups");
                System.IO.Directory.CreateDirectory(backupDirectory);
                string fileName = "Main-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity";
                System.IO.File.Copy(scenePath,
                    System.IO.Path.Combine(backupDirectory, fileName), false);
            }
            Generate();
        }

        [MenuItem("Fruit Tree/Generate Playable Scene")]
        public static void Generate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Fruit Tree Game");
            app.AddComponent<MobileBootstrap>();
            app.AddComponent<DevelopmentPerformanceMonitor>();
            var adsPolicy = app.AddComponent<AdsAccessPolicy>();
            var purchasing = app.AddComponent<RemoveAdsPurchaser>();
            purchasing.adsPolicy = adsPolicy;

            var root = new GameObject("Apple Tree");
            root.AddComponent<TreeGenerator>();
            var orchard = app.AddComponent<OrchardGameController>();
            orchard.tree = root.GetComponent<TreeGenerator>();

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
            var orbit = cameraObject.AddComponent<OrbitPickingCamera>();
            orbit.target = root.transform;
            orchard.orbitCamera = orbit;

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
            CreateInterface(root.GetComponent<TreeGenerator>(), sound, purchasing, orchard);
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
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            EnsureMaterial("VertexLitReference", "FruitTree/VertexLit");
            EnsureMaterial("SoftSkyReference", "FruitTree/SoftSky");
        }

        private static void EnsureMaterial(string materialName, string shaderName)
        {
            string path = "Assets/Resources/" + materialName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            string shaderPath = shaderName == "FruitTree/VertexLit" ?
                "Assets/Shaders/VertexLit.shader" : "Assets/Shaders/SoftSky.shader";
            // Resolve by file location first: it also works when a shader has not
            // yet been registered by Shader.Find during a fresh Unity import.
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
                shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            }
            if (shader == null)
                throw new System.InvalidOperationException("Fruit Tree shader import failed: " + shaderPath);
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

        private static void CreateInterface(TreeGenerator tree, OrchardAudio sound,
            RemoveAdsPurchaser purchasing, OrchardGameController orchard)
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

            var panel = new GameObject("Counter Background", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            panel.transform.SetParent(safe.transform, false);
            Anchor(panel.GetComponent<RectTransform>(),
                new Vector2(.355f, .903f), new Vector2(.645f, .966f));
            var counterFace = panel.GetComponent<RoundedPanel>();
            counterFace.color = new Color(.99f, .97f, .87f, .94f);
            counterFace.CornerRadius = 48f;
            counterFace.raycastTarget = false;
            var count = MakeLabel("Apple Counter", safe.transform, "0 / 30", 56,
                new Color(.21f, .32f, .19f),
                new Vector2(.355f, .903f), new Vector2(.645f, .966f));

            var hintBackground = new GameObject("Hint Background", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            hintBackground.transform.SetParent(safe.transform, false);
            Anchor(hintBackground.GetComponent<RectTransform>(),
                new Vector2(.15f, .048f), new Vector2(.85f, .12f));
            var hintFace = hintBackground.GetComponent<RoundedPanel>();
            hintFace.color = new Color(.14f, .28f, .19f, .75f);
            hintFace.CornerRadius = 40f;
            hintFace.raycastTarget = false;
            var hint = MakeLabel("Control Hint", safe.transform, "Drag to rotate  -  Tap apples", 40,
                new Color(.99f, .99f, .95f),
                new Vector2(.17f, .055f), new Vector2(.83f, .114f));
            var hud = canvasObject.AddComponent<HarvestHUD>();
            hud.tree = tree; hud.countText = count; hud.messageText = hint;
            hud.OrchardMode = true;
            orchard.hud = hud;
            orchard.treeLabel = MakeLabel("Current Tree Name", safe.transform,
                "APPLE TREE", 31, new Color(.21f, .32f, .19f),
                new Vector2(.24f, .856f), new Vector2(.76f, .898f));
            // Keep the gameplay clean. Only the small pause button is visible.
            // Restart and sound are accessible through the pause menu.
            var pauseButton = MakeButton("Pause", safe.transform, "II",
                new Vector2(.864f, .9f), new Vector2(.954f, .966f),
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
            var restart = MakeButton("Tree Collection", menu.transform, "TREES",
                new Vector2(.23f, .40f), new Vector2(.77f, .48f),
                new Color(.97f, .97f, .89f, 1f));
            var audio = MakeButton("Sound", menu.transform, "SOUND ON",
                new Vector2(.23f, .29f), new Vector2(.77f, .37f),
                new Color(.97f, .97f, .89f, 1f));
            sound.soundStatusText = audio.GetComponentInChildren<Text>();
            UnityEventTools.AddPersistentListener(pauseButton.onClick, pauseController.Toggle);
            UnityEventTools.AddPersistentListener(resume.onClick, pauseController.Resume);
            UnityEventTools.AddPersistentListener(restart.onClick, orchard.OpenTreeMenu);
            UnityEventTools.AddPersistentListener(audio.onClick, sound.ToggleSound);

            var parentButton = MakeButton("Parent Access", menu.transform,
                "FOR PARENTS - HOLD", new Vector2(.18f, .17f),
                new Vector2(.82f, .255f), new Color(.82f, .88f, .76f, 1f));
            // Parent panel is the final sibling so it overlays ordinary pause controls.
            var parentPanel = new GameObject("Parent Options", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            parentPanel.transform.SetParent(menu.transform, false);
            Anchor(parentPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            parentPanel.GetComponent<Image>().color = new Color(.15f, .21f, .17f, .98f);
            pauseController.parentPanel = parentPanel;

            var gate = parentButton.gameObject.AddComponent<ParentHoldGate>();
            gate.parentPanel = parentPanel;
            gate.label = parentButton.GetComponentInChildren<Text>();

            MakeLabel("Parent Heading", parentPanel.transform, "PARENT OPTIONS",
                62, Color.white, new Vector2(.12f, .72f), new Vector2(.88f, .81f));
            var statusText = MakeLabel("Store Message", parentPanel.transform,
                "Checking Google Play...", 32, new Color(.94f, .96f, .90f),
                new Vector2(.12f, .59f), new Vector2(.88f, .70f));
            var price = MakeLabel("Localized Store Price", parentPanel.transform,
                "Not available", 29, new Color(.98f, .94f, .72f),
                new Vector2(.20f, .50f), new Vector2(.80f, .56f));
            var buy = MakeButton("Remove Ads", parentPanel.transform,
                "REMOVE ADS", new Vector2(.22f, .39f),
                new Vector2(.78f, .47f), new Color(.96f, .96f, .88f, 1f));
            var restore = MakeButton("Restore Purchases", parentPanel.transform,
                "RESTORE PURCHASE", new Vector2(.22f, .28f),
                new Vector2(.78f, .36f), new Color(.96f, .96f, .88f, 1f));
            var close = MakeButton("Close Parent Options", parentPanel.transform,
                "BACK", new Vector2(.30f, .14f),
                new Vector2(.70f, .21f), new Color(.82f, .88f, .76f, 1f));
            var parentController = canvasObject.AddComponent<ParentPurchasePanel>();
            parentController.panel = parentPanel;
            purchasing.buyButton = buy;
            purchasing.restoreButton = restore;
            purchasing.statusText = statusText;
            purchasing.priceText = price;
            UnityEventTools.AddPersistentListener(buy.onClick, purchasing.BuyRemoveAds);
            UnityEventTools.AddPersistentListener(restore.onClick, purchasing.RestorePurchases);
            UnityEventTools.AddPersistentListener(close.onClick, parentController.Close);
            buy.interactable = false; // Ad SDK not installed: never charge for zero ads.
            restore.interactable = false;
            parentPanel.SetActive(false);

            // A dedicated collection overlay keeps the ordinary pause menu small.
            var collection = new GameObject("Tree Collection", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            collection.transform.SetParent(menu.transform, false);
            Anchor(collection.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            collection.GetComponent<Image>().color = new Color(.14f, .21f, .16f, .97f);
            pauseController.treePanel = collection;
            orchard.treeMenuPanel = collection;
            MakeLabel("Collection Heading", collection.transform, "MY TREES",
                66, Color.white, new Vector2(.15f, .73f), new Vector2(.85f, .83f));
            orchard.collectionStatus = MakeLabel("Tree Progress", collection.transform,
                "APPLE TREE", 38, new Color(.98f, .96f, .88f),
                new Vector2(.10f, .52f), new Vector2(.90f, .70f));
            orchard.previousButton = MakeButton("Previous Tree", collection.transform,
                "PREVIOUS", new Vector2(.14f, .41f), new Vector2(.48f, .49f),
                new Color(.96f, .97f, .88f, 1f));
            orchard.nextButton = MakeButton("Next Tree", collection.transform,
                "NEXT TREE", new Vector2(.52f, .41f), new Vector2(.86f, .49f),
                new Color(.96f, .97f, .88f, 1f));
            orchard.rebloomButton = MakeButton("Rebloom", collection.transform,
                "REBLOOM", new Vector2(.22f, .29f), new Vector2(.78f, .37f),
                new Color(.92f, .96f, .81f, 1f));
            var collectionBack = MakeButton("Back to Pause", collection.transform,
                "BACK", new Vector2(.29f, .17f), new Vector2(.71f, .25f),
                new Color(.82f, .88f, .76f, 1f));
            UnityEventTools.AddPersistentListener(orchard.previousButton.onClick, orchard.PreviousTree);
            UnityEventTools.AddPersistentListener(orchard.nextButton.onClick, orchard.NextTree);
            UnityEventTools.AddPersistentListener(orchard.rebloomButton.onClick, orchard.Rebloom);
            UnityEventTools.AddPersistentListener(collectionBack.onClick, orchard.CloseTreeMenu);
            collection.SetActive(false);

            // A calm completion shortcut, never displayed during ordinary harvesting.
            orchard.completionNextButton = MakeButton("Completion Next Tree",
                safe.transform, "NEXT TREE", new Vector2(.31f, .14f),
                new Vector2(.69f, .215f), new Color(.94f, .97f, .83f, .96f));
            UnityEventTools.AddPersistentListener(orchard.completionNextButton.onClick,
                orchard.NextTree);
            orchard.completionNextButton.gameObject.SetActive(false);

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
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel), typeof(Button));
            go.transform.SetParent(parent, false);
            Anchor(go.GetComponent<RectTransform>(), min, max);
            var face = go.GetComponent<RoundedPanel>();
            face.CornerRadius = 44f;
            face.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = face;
            var tint = button.colors;
            tint.highlightedColor = new Color(.94f, .96f, .88f);
            tint.pressedColor = new Color(.76f, .83f, .71f);
            button.colors = tint;
            MakeLabel(name + " Label", go.transform, text, 44, new Color(.22f,.30f,.19f), Vector2.zero, Vector2.one);
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
