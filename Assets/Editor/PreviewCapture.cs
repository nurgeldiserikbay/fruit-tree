#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Enters Play Mode and saves portrait screenshots of the Meadow theme to
    /// Builds/. Run in batch mode without -nographics and without -quit.
    /// </summary>
    [InitializeOnLoad]
    public static class PreviewCapture
    {
        private const string ActiveKey = "FruitTree.PreviewCapture.Active";
        private static int frames;

        static PreviewCapture()
        {
            if (SessionState.GetBool(ActiveKey, false))
                EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Opens the playable scene in the normal Editor window.</summary>
        public static void OpenMainScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            frames++;
            if (frames == 5)
            {
                var environment = Object.FindFirstObjectByType<EnvironmentController>();
                if (environment != null) environment.ApplyThemeDirect(EnvironmentTheme.Meadow);
                // Saved progress may be on another species; the preview judges
                // the apple slice. Regenerating does not write to the save file.
                var tree = Object.FindFirstObjectByType<TreeGenerator>();
                var apple = OrchardGameController.LevelDefinitions[0];
                if (tree != null) tree.Configure(apple.SpeciesId, apple.FruitCount, 122);
            }
            if (frames < 240) return;

            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            int exitCode = 0;
            try
            {
                Camera camera = Camera.main;
                SaveWithInterface(camera, "preview-home.png");
                var orchard = Object.FindFirstObjectByType<OrchardGameController>();
                var flow = Object.FindFirstObjectByType<FruitTreeScreenFlow>();
                if (orchard != null)
                {
                    orchard.OpenTreeMenu();
                    SaveWithInterface(camera, "preview-trees.png");
                    orchard.CloseTreeMenu();
                }
                // Each screen is opened the way a player opens it, then drawn once,
                // because Unity ignores taps on UI that has never been rendered.
                if (flow != null && flow.settingsPanel != null)
                {
                    flow.OpenSettingsFromHome();
                    CheckScreen(camera, flow.settingsPanel, "Settings (from home)");
                    SaveWithInterface(camera, "preview-settings.png");
                    SoundLevelsPanel.Open(flow.settingsPanel.transform.parent, null);
                    SaveWithInterface(camera, "preview-sound.png");
                    TapCheck(FindRoot("Sound Levels Screen"), "Sound levels");
                    // The real BACK button must close the window.
                    var soundBack = FindRoot("Sound Levels Screen")?.transform.Find("Back")?.GetComponent<UnityEngine.UI.Button>();
                    if (soundBack != null) soundBack.onClick.Invoke();
                    tapReport.AppendLine(SoundLevelsPanel.IsOpen ? "FAIL Sound levels BACK left the window open" :
                        "OK   Sound levels BACK closes the window");
                    SoundLevelsPanel.CloseIfOpen();
                    flow.CloseSettings();
                }
                if (flow != null && flow.homePanel != null) CheckScreen(camera, flow.homePanel, "Home");
                var pause = Object.FindFirstObjectByType<PauseController>();
                if (flow != null && orchard != null)
                {
                    flow.OpenCollectionFromHome(); // MY TREES on the home screen
                    CheckScreen(camera, FindRoot("Tree Collection Screen"), "Tree list (from home)");
                    orchard.CloseTreeMenu();
                    flow.ReturnHome();
                }
                if (flow != null)
                {
                    flow.Play();
                    if (flow.gameplayControls != null) CheckScreen(camera, flow.gameplayControls, "Game");
                    if (pause != null && pause.panel != null)
                    {
                        pause.Toggle();
                        CheckScreen(camera, pause.panel, "Pause");
                        flow.OpenSettingsFromPause();
                        CheckScreen(camera, flow.settingsPanel, "Settings (from pause)");
                        flow.CloseSettings();
                        orchard?.OpenTreeMenu();
                        CheckScreen(camera, FindRoot("Tree Collection Screen"), "Tree list (from pause)");
                        orchard?.CloseTreeMenu();
                        pause.Resume();
                    }
                    flow.ReturnHome();
                }
                File.WriteAllText(Path.Combine("Builds", "ui-tap-check.txt"), tapReport.ToString());
                var hud = Object.FindFirstObjectByType<HarvestHUD>();
                if (hud != null && flow != null)
                {
                    flow.Play();
                    hud.ShowReachHint(new Vector2(Screen.width * .5f, Screen.height * .5f));
                    SaveWithInterface(camera, "preview-reach.png");
                    flow.ReturnHome();
                }
                Save(camera, "preview-meadow.png");
                Vector3 startPosition = camera.transform.position;
                Quaternion startRotation = camera.transform.rotation;
                // Low side view: shows how the panorama meets the ground.
                camera.transform.position = new Vector3(3.4f, 1.5f, -7f);
                camera.transform.LookAt(new Vector3(-1.2f, 3.1f, 0f));
                Save(camera, "preview-meadow-forest.png");
                var weather = Object.FindFirstObjectByType<WeatherController>();
                if (weather != null)
                {
                    WeatherKind opening = weather.Current;
                    foreach (WeatherKind kind in System.Enum.GetValues(typeof(WeatherKind)))
                    {
                        weather.ForceWeather(kind);
                        Save(camera, "preview-weather-" + kind.ToString().ToLowerInvariant() + ".png");
                    }
                    weather.ForceWeather(opening);
                }
                var speciesTree = Object.FindFirstObjectByType<TreeGenerator>();
                if (speciesTree != null)
                {
                    camera.transform.position = new Vector3(2.4f, 2.1f, -8.6f);
                    camera.transform.LookAt(new Vector3(0f, 3.2f, 0f));
                    Vector3 overview = camera.transform.position;
                    Quaternion overviewRotation = camera.transform.rotation;
                    foreach (var level in OrchardGameController.LevelDefinitions)
                    {
                        speciesTree.Configure(level.SpeciesId, level.FruitCount, 122);
                        CrownShadow.RebuildNow();
                        camera.transform.SetPositionAndRotation(overview, overviewRotation);
                        Save(camera, "preview-species-" + level.SpeciesId + ".png");
                        // Close-up of one outer fruit, to judge the peel.
                        Apple close = null;
                        float best = -1f;
                        foreach (Apple fruit in speciesTree.Apples)
                        {
                            if (fruit == null) continue;
                            Vector3 p = fruit.transform.position;
                            float score = new Vector2(p.x, p.z).magnitude - Mathf.Abs(p.y - 2.6f) * .5f - Mathf.Abs(p.x) * .3f + (p.z < 0f ? 1f : 0f);
                            if (score > best) { best = score; close = fruit; }
                        }
                        if (close != null)
                        {
                            Vector3 fruitPoint = close.transform.position;
                            Vector3 outward = new Vector3(fruitPoint.x, 0f, fruitPoint.z).normalized;
                            camera.transform.position = fruitPoint + outward * .55f + Vector3.up * .08f;
                            camera.transform.LookAt(fruitPoint);
                            Save(camera, "preview-fruit-" + level.SpeciesId + ".png");
                        }
                    }
                    camera.transform.SetPositionAndRotation(overview, overviewRotation);
                    var firstLevel = OrchardGameController.LevelDefinitions[0];
                    speciesTree.Configure(firstLevel.SpeciesId, firstLevel.FruitCount, 122);
                    CrownShadow.RebuildNow();
                }
                var scenery = Object.FindFirstObjectByType<EnvironmentController>();
                if (scenery != null)
                {
                    if (weather != null) weather.ForceWeather(WeatherKind.Clear);
                    camera.transform.position = new Vector3(3.4f, 1.9f, -7f);
                    camera.transform.LookAt(new Vector3(-1.2f, 2.6f, 0f));
                    for (int index = 0; index < EnvironmentThemes.Count; index++)
                    {
                        var theme = (EnvironmentTheme)index;
                        scenery.ApplyThemeDirect(theme);
                        Save(camera, "preview-theme-" + theme + ".png");
                    }
                    var wildlife = Object.FindFirstObjectByType<Wildlife>();
                    foreach (var theme in new[] { EnvironmentTheme.Meadow, EnvironmentTheme.JapaneseGarden })
                    {
                        scenery.ApplyThemeDirect(theme);
                        if (wildlife != null) wildlife.Showcase(camera);
                        Save(camera, "preview-nature-" + theme + ".png");
                    }
                    scenery.ApplyThemeDirect(EnvironmentTheme.Meadow);
                }
                camera.transform.SetPositionAndRotation(startPosition, startRotation);
                Vector3 focus = new Vector3(0f, 3f, 0f);
                camera.transform.position = Vector3.Lerp(camera.transform.position, focus, .48f);
                camera.transform.LookAt(focus);
                Save(camera, "preview-meadow-close.png");
                // Fruit-only view separates fruit mesh defects from leaf occlusion.
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if (renderer.name == "Merged Leaves") renderer.enabled = false;
                Save(camera, "preview-meadow-fruit.png");
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static readonly System.Text.StringBuilder tapReport = new System.Text.StringBuilder();

        private static void CheckScreen(Camera camera, GameObject root, string label)
        {
            SaveWithInterface(camera, "tap-check-frame.png");
            TapCheck(root, label);
        }

        private static GameObject FindRoot(string name)
        {
            foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                if (rect.name == name) return rect.gameObject;
            return null;
        }

        /// <summary>
        /// A real tap test: for every visible, enabled button and slider, ask the
        /// EventSystem what a finger at its centre would hit. Anything else on top,
        /// or nothing at all, means the control cannot be pressed there.
        /// </summary>
        private static void TapCheck(GameObject root, string label)
        {
            if (root == null) { tapReport.AppendLine("FAIL " + label + ": screen not found"); return; }
            Canvas.ForceUpdateCanvases();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null) { tapReport.AppendLine("FAIL no EventSystem"); return; }
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            foreach (var control in root.GetComponentsInChildren<UnityEngine.UI.Selectable>())
            {
                if (!control.IsInteractable() || !control.isActiveAndEnabled) continue;
                var rect = (RectTransform)control.transform;
                Canvas canvas = control.GetComponentInParent<Canvas>().rootCanvas;
                Camera eye = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector3 centre = rect.TransformPoint(rect.rect.center);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(eye, centre);
                // Off-screen parts of a scrolling list are not tappable by design.
                if (screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height) continue;
                results.Clear();
                events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = screen }, results);
                string name = label + " / " + control.name +
                    (control.GetComponentInChildren<UnityEngine.UI.Text>() is UnityEngine.UI.Text t && t.text.Length > 0 ? " \"" + t.text + "\"" : "");
                if (results.Count == 0) { tapReport.AppendLine("FAIL " + name + ": nothing receives the tap"); continue; }
                var hit = results[0].gameObject;
                bool reaches = hit.transform.IsChildOf(control.transform) ||
                    hit.GetComponentInParent<UnityEngine.UI.Selectable>() == control;
                tapReport.AppendLine((reaches ? "OK   " : "FAIL ") + name +
                    (reaches ? "" : ": blocked by " + hit.name));
            }
        }

        /// <summary>Overlay canvases skip Camera.Render, so route them through the camera once.</summary>
        private static void SaveWithInterface(Camera camera, string fileName)
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
            try { Save(camera, fileName); }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i].isRootCanvas) canvases[i].renderMode = modes[i];
            }
        }

        private static void Save(Camera camera, string fileName)
        {
            const int width = 720, height = 1600;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Builds");
                File.WriteAllBytes(Path.Combine("Builds", fileName), image.EncodeToPNG());
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
