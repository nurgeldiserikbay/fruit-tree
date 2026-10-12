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
            var treeBuilder = root.AddComponent<TreeGenerator>();
            treeBuilder.SetAutomaticGeneration(false); // Orchard.Start supplies saved species.
            var orchard = app.AddComponent<OrchardGameController>();
            orchard.tree = root.GetComponent<TreeGenerator>();

            EnsureShaderIsInBuild();
            var ground = new GameObject("Quiet Meadow").AddComponent<GroundGenerator>();
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
            var environment = ground.gameObject.AddComponent<EnvironmentController>();
            environment.ground = ground;
            var skyRoot = GameObject.Find("Soft Sky");
            environment.skyRenderer = skyRoot != null ? skyRoot.GetComponent<Renderer>() : null;
            environment.sun = light;
            orchard.environment = environment;
            // Set a fixed, inexpensive quality budget once per launch on Android.
            var mobileBudget = ground.gameObject.AddComponent<MobilePerformanceBudget>();
            mobileBudget.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.68f, .73f, .77f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogStartDistance = 13f;
            RenderSettings.fogEndDistance = 28f;

            var sound = new GameObject("Orchard Sounds").AddComponent<OrchardAudio>();
            sound.tree = root.GetComponent<TreeGenerator>();
            sound.environment = environment;
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

            var gameLayer = new GameObject("Gameplay Controls", typeof(RectTransform));
            gameLayer.transform.SetParent(safe.transform, false);
            Anchor(gameLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var gameRoot = gameLayer.transform;

            var panel = new GameObject("Counter Background", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            panel.transform.SetParent(gameRoot, false);
            Anchor(panel.GetComponent<RectTransform>(),
                new Vector2(.355f, .903f), new Vector2(.645f, .966f));
            var counterFace = panel.GetComponent<RoundedPanel>();
            counterFace.color = new Color(.99f, .97f, .87f, .94f);
            counterFace.CornerRadius = 48f;
            counterFace.raycastTarget = false;
            var count = MakeLabel("Apple Counter", gameRoot, "0 / 30", 56,
                new Color(.21f, .32f, .19f),
                new Vector2(.355f, .903f), new Vector2(.645f, .966f));

            var hintBackground = new GameObject("Hint Background", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            hintBackground.transform.SetParent(gameRoot, false);
            Anchor(hintBackground.GetComponent<RectTransform>(),
                new Vector2(.15f, .048f), new Vector2(.85f, .12f));
            var hintFace = hintBackground.GetComponent<RoundedPanel>();
            hintFace.color = new Color(.14f, .28f, .19f, .75f);
            hintFace.CornerRadius = 40f;
            hintFace.raycastTarget = false;
            var hint = MakeLabel("Control Hint", gameRoot, "Drag to rotate  -  Tap apples", 40,
                new Color(.99f, .99f, .95f),
                new Vector2(.17f, .055f), new Vector2(.83f, .114f));
            var hud = canvasObject.AddComponent<HarvestHUD>();
            hud.tree = tree; hud.countText = count; hud.messageText = hint;
            hud.OrchardMode = true;
            orchard.hud = hud;
            orchard.treeLabel = MakeLabel("Current Tree Name", gameRoot,
                "APPLE TREE", 31, new Color(.21f, .32f, .19f),
                new Vector2(.24f, .856f), new Vector2(.76f, .898f));
            // Thin, non-interactive progress line; uses the existing harvest
            // event, not a frame-by-frame update or per-fruit UI objects.
            var progressTrack = new GameObject("Harvest Progress Track",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedPanel));
            progressTrack.transform.SetParent(gameRoot, false);
            Anchor(progressTrack.GetComponent<RectTransform>(),
                new Vector2(.32f, .83f), new Vector2(.68f, .84f));
            var trackGraphic = progressTrack.GetComponent<RoundedPanel>();
            trackGraphic.color = new Color(.15f, .27f, .19f, .24f);
            trackGraphic.CornerRadius = 9f;
            trackGraphic.raycastTarget = false;
            var progressFill = new GameObject("Harvest Progress Fill",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedPanel));
            progressFill.transform.SetParent(progressTrack.transform, false);
            Anchor(progressFill.GetComponent<RectTransform>(),
                Vector2.zero, new Vector2(0f, 1f));
            var fillGraphic = progressFill.GetComponent<RoundedPanel>();
            fillGraphic.color = new Color(.86f, .77f, .43f);
            fillGraphic.CornerRadius = 9f;
            fillGraphic.raycastTarget = false;
            var progressIndicator = canvasObject.AddComponent<OrchardProgressBar>();
            progressIndicator.tree = tree;
            progressIndicator.fill = progressFill.GetComponent<RectTransform>();

            // Three pooled non-blocking glints in a stretched full-screen overlay.
            // They appear only on genuine fruit taps and never intercept input.
            var pickEffectOverlay = new GameObject("Fruit Pick Glints",
                typeof(RectTransform));
            pickEffectOverlay.transform.SetParent(gameRoot, false);
            Anchor(pickEffectOverlay.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one);
            var pickEffectGroups = new CanvasGroup[3];
            for (int i = 0; i < pickEffectGroups.Length; i++)
            {
                var sparkle = new GameObject("Harvest Glow " + (i + 1),
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(RoundedPanel), typeof(CanvasGroup));
                sparkle.transform.SetParent(pickEffectOverlay.transform, false);
                var rect = sparkle.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = Vector2.one * (i == 1 ? 21f : 15f);
                var graphic = sparkle.GetComponent<RoundedPanel>();
                graphic.CornerRadius = 32f;
                graphic.color = i == 1 ?
                    new Color(1f, .93f, .66f) : new Color(.99f, .97f, .86f);
                graphic.raycastTarget = false;
                var group = sparkle.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                pickEffectGroups[i] = group;
            }
            var fruitPulse = canvasObject.AddComponent<FruitPickPulse>();
            fruitPulse.tree = tree;
            fruitPulse.worldCamera = orchard.orbitCamera.GetComponent<Camera>();
            fruitPulse.overlay = pickEffectOverlay.GetComponent<RectTransform>();
            fruitPulse.glints = pickEffectGroups;

            // Keep the gameplay clean. Only the small pause button is visible.
            // Restart and sound are accessible through the pause menu.
            var pauseButton = MakeButton("Pause", gameRoot, "II",
                new Vector2(.864f, .9f), new Vector2(.954f, .966f),
                new Color(.98f, .94f, .84f, .91f));

            var menu = new GameObject("Pause Menu", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            menu.transform.SetParent(safe.transform, false);
            Anchor(menu.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            menu.GetComponent<Image>().color = new Color(.08f, .15f, .12f, .57f);
            var pauseController = canvasObject.AddComponent<PauseController>();
            var screenFlow = canvasObject.AddComponent<FruitTreeScreenFlow>();
            screenFlow.pause = pauseController;
            screenFlow.orchard = orchard;
            screenFlow.orbitCamera = orchard.orbitCamera;
            screenFlow.sound = sound;
            screenFlow.activeTree = tree;
            screenFlow.environment = orchard.environment;
            screenFlow.gameplayControls = gameLayer;
            pauseController.screenFlow = screenFlow;
            pauseController.panel = menu;
            pauseController.hud = hud;
            orchard.pauseController = pauseController;

            MakeDialogSurface("Pause Calm Card", menu.transform,
                new Vector2(.105f, .07f), new Vector2(.895f, .91f));
            MakeLabel("Paused Title", menu.transform, "PAUSED", 70, Color.white,
                new Vector2(.17f, .78f), new Vector2(.83f, .87f));
            var resume = MakeButton("Resume", menu.transform, "CONTINUE",
                new Vector2(.23f, .66f), new Vector2(.77f, .735f),
                new Color(.97f, .97f, .89f, 1f));
            var restart = MakeButton("Tree Collection", menu.transform, "TREES",
                new Vector2(.23f, .555f), new Vector2(.77f, .63f),
                new Color(.97f, .97f, .89f, 1f));
            var audio = MakeButton("Sound", menu.transform, "SOUND ON",
                new Vector2(.23f, .37f), new Vector2(.77f, .44f),
                new Color(.97f, .97f, .89f, 1f));
            var zenMode = MakeButton("Enter Zen Mode", menu.transform, "ZEN MODE",
                new Vector2(.23f, .285f), new Vector2(.77f, .355f),
                new Color(.90f, .96f, .81f, 1f));
            sound.soundStatusText = audio.GetComponentInChildren<Text>();
            UnityEventTools.AddPersistentListener(pauseButton.onClick, pauseController.Toggle);
            UnityEventTools.AddPersistentListener(resume.onClick, pauseController.Resume);
            UnityEventTools.AddPersistentListener(restart.onClick, orchard.OpenTreeMenu);
            UnityEventTools.AddPersistentListener(audio.onClick, sound.ToggleSound);
            UnityEventTools.AddPersistentListener(zenMode.onClick, screenFlow.EnterZenMode);

            var settingsInPause = MakeButton("Pause Settings", menu.transform,
                "SETTINGS", new Vector2(.23f, .45f), new Vector2(.77f, .525f),
                new Color(.97f, .97f, .89f, 1f));
            var homeInPause = MakeButton("Pause Main Menu", menu.transform,
                "MAIN MENU", new Vector2(.23f, .205f), new Vector2(.77f, .275f),
                new Color(.97f, .97f, .89f, 1f));
            UnityEventTools.AddPersistentListener(settingsInPause.onClick,
                screenFlow.OpenSettingsFromPause);
            UnityEventTools.AddPersistentListener(homeInPause.onClick,
                screenFlow.ReturnHome);

            var parentButton = MakeButton("Parent Access", menu.transform,
                "FOR PARENTS - HOLD", new Vector2(.18f, .085f),
                new Vector2(.82f, .165f), new Color(.82f, .88f, .76f, 1f));
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
            collection.GetComponent<Image>().color = new Color(.08f, .15f, .12f, .66f);
            pauseController.treePanel = collection;
            orchard.treeMenuPanel = collection;
            MakeDialogSurface("Collection Calm Card", collection.transform,
                new Vector2(.09f, .15f), new Vector2(.91f, .875f));
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

            // An optional last-fruit helper. It turns the view rather than picking.
            orchard.remainingFruitHintButton = MakeButton("Find Remaining Fruit",
                gameRoot, "FIND FRUIT", new Vector2(.67f, .145f),
                new Vector2(.94f, .205f), new Color(.91f, .96f, .82f, .93f));
            UnityEventTools.AddPersistentListener(orchard.remainingFruitHintButton.onClick,
                orchard.FindRemainingFruit);
            orchard.remainingFruitHintButton.gameObject.SetActive(false);

            // A soft, non-blocking moment of satisfaction after the final pick.
            var clearNotice = new GameObject("Quiet Harvest Completion",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel), typeof(CanvasGroup));
            clearNotice.transform.SetParent(gameRoot, false);
            Anchor(clearNotice.GetComponent<RectTransform>(),
                new Vector2(.18f, .255f), new Vector2(.82f, .316f));
            var noticeFace = clearNotice.GetComponent<RoundedPanel>();
            noticeFace.color = new Color(.18f, .31f, .23f, .74f);
            noticeFace.CornerRadius = 36f;
            noticeFace.raycastTarget = false;
            var noticeGroup = clearNotice.GetComponent<CanvasGroup>();
            noticeGroup.alpha = 0f;
            noticeGroup.interactable = false;
            noticeGroup.blocksRaycasts = false;
            var noticeLabel = MakeLabel("Completion Message", clearNotice.transform,
                "EVERY FRUIT COLLECTED", 38, new Color(.98f, .96f, .86f),
                Vector2.zero, Vector2.one);
            var completionFeedback = canvasObject.AddComponent<CalmCompletionFeedback>();
            completionFeedback.orchard = orchard;
            completionFeedback.messageGroup = noticeGroup;
            completionFeedback.message = noticeLabel;

            // A calm completion shortcut, never displayed during ordinary harvesting.
            orchard.completionNextButton = MakeButton("Completion Next Tree",
                gameRoot, "NEXT TREE", new Vector2(.31f, .14f),
                new Vector2(.69f, .215f), new Color(.94f, .97f, .83f, .96f));
            UnityEventTools.AddPersistentListener(orchard.completionNextButton.onClick,
                orchard.NextTree);
            orchard.completionNextButton.gameObject.SetActive(false);
            // Keep pause and parent overlays visually above the completion shortcut.
            orchard.completionNextButton.transform.SetSiblingIndex(menu.transform.GetSiblingIndex());

            // Zen Mode intentionally hides every HUD control, but an always
            // visible tiny exit button gives a way back without Android Back.
            var zenExit = new GameObject("Zen Exit Controls", typeof(RectTransform));
            zenExit.transform.SetParent(safe.transform, false);
            Anchor(zenExit.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            screenFlow.zenExitPanel = zenExit;
            var exitZen = MakeButton("Leave Zen Mode", zenExit.transform,
                "EXIT ZEN", new Vector2(.36f, .914f),
                new Vector2(.64f, .965f), new Color(.98f, .97f, .88f, .65f));
            UnityEventTools.AddPersistentListener(exitZen.onClick, screenFlow.ExitZenMode);
            zenExit.SetActive(false);

            BuildFrontEnd(safe.transform, screenFlow);

            menu.SetActive(false);
        }

        private static GameObject MakeOverlay(string name, Transform parent, Color tint)
        {
            var overlay = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(parent, false);
            Anchor(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            overlay.GetComponent<Image>().color = tint;
            return overlay;
        }

        private static void BuildFrontEnd(Transform safe, FruitTreeScreenFlow flow)
        {
            var ivory = new Color(.97f, .965f, .89f, 1f);
            var fern = new Color(.82f, .89f, .76f, 1f);
            var white = new Color(.98f, .98f, .95f, 1f);
            var dim = new Color(.07f, .16f, .12f, .60f);

            // The orchard remains visible behind a light darkening veil;
            // only the logo section needs its own translucent reading surface.
            var home = MakeOverlay("Home Screen", safe, new Color(.06f, .14f, .10f, .38f));
            flow.homePanel = home;
            var hero = new GameObject("Home Orchard Title Card",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedPanel));
            hero.transform.SetParent(home.transform, false);
            Anchor(hero.GetComponent<RectTransform>(),
                new Vector2(.075f, .635f), new Vector2(.925f, .867f));
            var heroFace = hero.GetComponent<RoundedPanel>();
            heroFace.CornerRadius = 64f;
            heroFace.color = new Color(.10f, .24f, .17f, .75f);
            heroFace.raycastTarget = false;
            MakeLabel("Home Eyebrow", hero.transform, "A LITTLE ORCHARD, JUST FOR YOU",
                28, new Color(.90f, .88f, .68f),
                new Vector2(.08f, .69f), new Vector2(.92f, .90f));
            MakeLabel("Game Logo", hero.transform, "FRUIT TREE", 104, white,
                new Vector2(.05f, .32f), new Vector2(.95f, .72f));
            MakeLabel("Home Tagline", hero.transform,
                "Take your time. Enjoy the trees.", 36, white,
                new Vector2(.07f, .10f), new Vector2(.93f, .35f));
            flow.homeProgressLabel = MakeLabel("Home Saved Harvest", home.transform,
                "TREE 1 / 10     0 / 120 FRUITS", 31, white,
                new Vector2(.10f, .565f), new Vector2(.90f, .623f));
            var play = MakeButton("Home Play", home.transform, "CONTINUE",
                new Vector2(.18f, .47f), new Vector2(.82f, .55f),
                new Color(.97f, .86f, .61f, 1f));
            var collection = MakeButton("Home Collection", home.transform, "MY TREES",
                new Vector2(.18f, .367f), new Vector2(.82f, .447f), ivory);
            var settings = MakeButton("Home Settings", home.transform, "SETTINGS",
                new Vector2(.18f, .264f), new Vector2(.82f, .344f), ivory);
            var help = MakeButton("Home How To Play", home.transform, "HOW TO PLAY",
                new Vector2(.18f, .161f), new Vector2(.82f, .241f), fern);
            MakeLabel("Home Footer", home.transform,
                "10 FRUIT TREES  •  4 PEACEFUL LANDSCAPES", 29, white,
                new Vector2(.07f, .06f), new Vector2(.93f, .11f));
            UnityEventTools.AddPersistentListener(play.onClick, flow.Play);
            UnityEventTools.AddPersistentListener(collection.onClick,
                flow.OpenCollectionFromHome);
            UnityEventTools.AddPersistentListener(settings.onClick,
                flow.OpenSettingsFromHome);
            UnityEventTools.AddPersistentListener(help.onClick, flow.OpenHelp);

            var options = MakeOverlay("Settings Screen", safe, dim);
            flow.settingsPanel = options;
            MakeDialogSurface("Settings Calm Card", options.transform,
                new Vector2(.10f, .07f), new Vector2(.90f, .89f));
            MakeLabel("Settings Title", options.transform, "SETTINGS", 78, white,
                new Vector2(.12f, .74f), new Vector2(.88f, .84f));
            var sound = MakeButton("Settings Sound", options.transform, "SOUND: ON",
                new Vector2(.20f, .665f), new Vector2(.80f, .735f), ivory);
            flow.audioButtonLabel = sound.GetComponentInChildren<Text>();
            var softAudio = MakeButton("Settings Quiet Audio", options.transform,
                "AUDIO: SOFT", new Vector2(.20f, .575f), new Vector2(.80f, .645f), ivory);
            flow.audioLevelLabel = softAudio.GetComponentInChildren<Text>();
            var motion = MakeButton("Settings Motion", options.transform,
                "MOTION: GENTLE", new Vector2(.20f, .485f), new Vector2(.80f, .555f), ivory);
            flow.motionLabel = motion.GetComponentInChildren<Text>();
            var scenery = MakeButton("Settings Scenery", options.transform,
                "SCENERY: AUTO", new Vector2(.20f, .395f), new Vector2(.80f, .465f), ivory);
            flow.sceneryLabel = scenery.GetComponentInChildren<Text>();
            var policy = MakeButton("Settings Privacy", options.transform, "PRIVACY POLICY",
                new Vector2(.20f, .305f), new Vector2(.80f, .375f), ivory);
            var controls = MakeButton("Settings Controls", options.transform, "HOW TO PLAY",
                new Vector2(.20f, .215f), new Vector2(.80f, .285f), ivory);
            var back = MakeButton("Settings Back", options.transform, "BACK",
                new Vector2(.27f, .105f), new Vector2(.73f, .175f), fern);
            UnityEventTools.AddPersistentListener(sound.onClick, flow.ToggleSound);
            UnityEventTools.AddPersistentListener(softAudio.onClick, flow.ToggleQuietAudio);
            UnityEventTools.AddPersistentListener(motion.onClick, flow.ToggleReducedMotion);
            UnityEventTools.AddPersistentListener(scenery.onClick, flow.CycleScenery);
            UnityEventTools.AddPersistentListener(policy.onClick, flow.OpenPrivacy);
            UnityEventTools.AddPersistentListener(controls.onClick, flow.OpenHelp);
            UnityEventTools.AddPersistentListener(back.onClick, flow.CloseSettings);

            var helpScreen = MakeOverlay("How To Play Screen", safe, dim);
            flow.helpPanel = helpScreen;
            MakeDialogSurface("Help Calm Card", helpScreen.transform,
                new Vector2(.085f, .11f), new Vector2(.915f, .895f));
            MakeLabel("Help Title", helpScreen.transform, "HOW TO PLAY", 70, white,
                new Vector2(.10f, .75f), new Vector2(.90f, .86f));
            MakeLabel("Help Content", helpScreen.transform,
                "Swipe to turn the tree\n\nPinch to zoom in or out\n\nTap fruit to collect it\n\nClear every fruit to unlock the next tree\n\nRebloom an old tree whenever you wish\n\nThere is no timer. Enjoy the moment.",
                42, white, new Vector2(.10f, .28f), new Vector2(.90f, .73f));
            var helpBack = MakeButton("Help Back", helpScreen.transform, "BACK",
                new Vector2(.27f, .15f), new Vector2(.73f, .23f), fern);
            UnityEventTools.AddPersistentListener(helpBack.onClick, flow.CloseHelp);

            var privacy = MakeOverlay("Privacy Information Screen", safe, dim);
            flow.privacyPanel = privacy;
            MakeDialogSurface("Privacy Calm Card", privacy.transform,
                new Vector2(.065f, .05f), new Vector2(.935f, .91f));
            MakeLabel("Privacy Heading", privacy.transform, "PRIVACY POLICY", 68, white,
                new Vector2(.09f, .78f), new Vector2(.91f, .88f));
            MakeLabel("Privacy Summary", privacy.transform,
                "Fruit Tree stores your game progress locally on your device.\n\n" +
                "The game has no account registration. Google Play may process purchases.\n\n" +
                "No advertising SDK is enabled in this build.\n\n" +
                "Read the full published policy for contact and retention details.",
                39, white, new Vector2(.09f, .37f), new Vector2(.91f, .75f));
            flow.privacyStatusLabel = MakeLabel("Privacy Status", privacy.transform,
                "Online policy will be available before release.", 30, white,
                new Vector2(.10f, .28f), new Vector2(.90f, .36f));
            flow.openPublishedPrivacyButton = MakeButton("Open Privacy Website",
                privacy.transform, "OPEN ONLINE POLICY",
                new Vector2(.17f, .18f), new Vector2(.83f, .26f), ivory);
            var privacyBack = MakeButton("Privacy Back", privacy.transform, "BACK",
                new Vector2(.29f, .075f), new Vector2(.71f, .15f), fern);
            UnityEventTools.AddPersistentListener(
                flow.openPublishedPrivacyButton.onClick, flow.OpenPublicPrivacyUrl);
            UnityEventTools.AddPersistentListener(privacyBack.onClick, flow.ClosePrivacy);
            flow.openPublishedPrivacyButton.interactable = false;

            options.SetActive(false);
            helpScreen.SetActive(false);
            privacy.SetActive(false);
            home.SetActive(true);
        }

        // A single non-interactive visual surface under each overlay's controls.
        // A rounded panel uses no sprite, separate texture, or third-party UI asset.
        private static void MakeDialogSurface(string name, Transform parent,
            Vector2 min, Vector2 max)
        {
            var card = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            card.transform.SetParent(parent, false);
            Anchor(card.GetComponent<RectTransform>(), min, max);
            var surface = card.GetComponent<RoundedPanel>();
            surface.CornerRadius = 66f;
            surface.color = new Color(.12f, .255f, .18f, .86f);
            surface.raycastTarget = false;
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
