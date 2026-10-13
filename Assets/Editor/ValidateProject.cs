#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FruitTree.Editor
{
    /// <summary>
    /// Inspect the generated game scene and all required references.
    /// Useful for first import and batchmode smoke tests without a human player.
    /// </summary>
    public static class ValidateProject
    {
        [MenuItem("Fruit Tree/Validate Project")]
        public static void Run()
        {
            const string path = "Assets/Scenes/Main.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                FruitTreeSceneBuilder.Generate();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var errors = new List<string>();

            var version = UnityEngine.Object.FindAnyObjectByType<FruitTreeSceneVersion>();
            if (version == null || !version.IsCurrent)
                errors.Add("Generated scene is outdated. Stop Play and use " +
                    "Fruit Tree > Refresh Visuals (Back Up Scene), then validate again.");

            var root = GameObject.Find("Apple Tree");
            var tree = root != null ? root.GetComponent<TreeGenerator>() : null;
            if (tree == null) errors.Add("Apple Tree or TreeGenerator is missing");

            var camera = Camera.main;
            if (camera == null || camera.GetComponent<OrbitPickingCamera>() == null)
                errors.Add("Main camera or orbit input component is missing");

            var hudObject = GameObject.Find("HUD");
            var hud = hudObject != null ? hudObject.GetComponent<HarvestHUD>() : null;
            if (hud == null || hud.tree != tree || hud.countText == null)
                errors.Add("Harvest HUD has missing references");

            var pause = hudObject != null ? hudObject.GetComponent<PauseController>() : null;
            if (pause == null || pause.panel == null || pause.parentPanel == null || pause.hud != hud)
                errors.Add("Pause menu has missing references");

            if (FruitTreeReleaseConfig.LanguageCode != "en")
                errors.Add("First release supports English UI only.");

            // Reject accidentally imported Russian/Kazakh labels in the playable
            // scene. This does not restrict the language of developer docs.
            foreach (var uiText in UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (uiText == null || string.IsNullOrEmpty(uiText.text)) continue;
                foreach (char ch in uiText.text)
                    if (ch >= '\u0400' && ch <= '\u052F')
                    {
                        errors.Add("Non-English game UI label: " + uiText.name);
                        break;
                    }
            }

            var purchasing = UnityEngine.Object.FindAnyObjectByType<RemoveAdsPurchaser>();
            var adsPolicy = UnityEngine.Object.FindAnyObjectByType<AdsAccessPolicy>();
            if (purchasing == null || adsPolicy == null || purchasing.adsPolicy != adsPolicy ||
                purchasing.buyButton == null || purchasing.restoreButton == null ||
                purchasing.statusText == null || purchasing.priceText == null)
                errors.Add("Backend-free IAP/ads policy references are missing");

            if (!FruitTreeReleaseConfig.AdvertisingEnabled &&
                (purchasing != null && purchasing.enabled))
                errors.Add("Ad-free release must never initialize Google Play Billing.");
            if (FruitTreeReleaseConfig.CanOfferRemoveAds(false))
                errors.Add("Remove Ads cannot be sold while no real ad provider exists.");

            var orchard = UnityEngine.Object.FindAnyObjectByType<OrchardGameController>();
            if (orchard == null || orchard.tree != tree || orchard.hud != hud ||
                orchard.treeMenuPanel == null || orchard.nextButton == null ||
                orchard.previousButton == null || orchard.rebloomButton == null ||
                orchard.completionNextButton == null ||
                orchard.remainingFruitHintButton == null ||
                orchard.pauseController != pause ||
                pause == null || pause.orchard != orchard)
                errors.Add("Orchard progression controller or collection UI is missing");

            var screenFlow = UnityEngine.Object.FindAnyObjectByType<FruitTreeScreenFlow>();
            if (screenFlow == null || screenFlow.homePanel == null ||
                screenFlow.gameplayControls == null || screenFlow.settingsPanel == null ||
                screenFlow.helpPanel == null || screenFlow.privacyPanel == null ||
                screenFlow.pause != pause || screenFlow.orchard != orchard ||
                pause.screenFlow != screenFlow ||
                screenFlow.openPublishedPrivacyButton == null ||
                screenFlow.homeProgressLabel == null ||
                screenFlow.audioLevelLabel == null || screenFlow.musicButtonLabel == null ||
                screenFlow.motionLabel == null ||
                screenFlow.sceneryLabel == null || screenFlow.environment == null ||
                screenFlow.zenExitPanel == null ||
                screenFlow.activeTree != tree)
                errors.Add("Home menu, Settings, Help, Privacy or flow references are missing");

            if (!FruitTreePrivacyConfig.IsPublishedUrlValid(
                FruitTreePrivacyConfig.PublishedPolicyUrl))
                Debug.LogWarning("Fruit Tree: public Google Docs privacy URL is not configured. " +
                    "This is acceptable for local Editor tests but BLOCKS Play Store release.");

            var environment = UnityEngine.Object.FindAnyObjectByType<EnvironmentController>();
            var terrain = UnityEngine.Object.FindAnyObjectByType<GroundGenerator>();
            if (environment == null || terrain == null ||
                environment.ground != terrain || environment.skyRenderer == null ||
                environment.sun == null || orchard == null ||
                orchard.environment != environment || screenFlow == null ||
                screenFlow.environment != environment)
                errors.Add("Lightweight scenery theme controller and UI wiring are missing");

            var budget = UnityEngine.Object.FindAnyObjectByType<MobilePerformanceBudget>();
            if (budget == null || environment == null || budget.sun != environment.sun)
                errors.Add("Android graphics quality budget is not wired to the scene sun");

            var progressBar = UnityEngine.Object.FindAnyObjectByType<OrchardProgressBar>();
            if (progressBar == null || progressBar.tree != tree || progressBar.fill == null)
                errors.Add("Gameplay harvest progress strip is not connected");

            var pulse = UnityEngine.Object.FindAnyObjectByType<FruitPickPulse>();
            if (pulse == null || pulse.tree != tree ||
                pulse.worldCamera != Camera.main || pulse.overlay == null ||
                pulse.glints == null || pulse.glints.Length != 3)
                errors.Add("Fruit-pick pooled visual pulse is not connected");
            else
            {
                foreach (var glint in pulse.glints)
                    if (glint == null || glint.blocksRaycasts ||
                        glint.GetComponent<RoundedPanel>() == null ||
                        glint.GetComponent<RoundedPanel>().raycastTarget)
                        errors.Add("Fruit-pick glints must never block input");
            }

            var fruitGuide = UnityEngine.Object.FindAnyObjectByType<RemainingFruitGuide>();
            if (fruitGuide == null || orchard == null ||
                orchard.remainingFruitGuide != fruitGuide ||
                fruitGuide.worldCamera != Camera.main ||
                fruitGuide.overlay == null || fruitGuide.marker == null ||
                fruitGuide.markerGroup == null ||
                fruitGuide.markerGroup.blocksRaycasts ||
                fruitGuide.marker.GetComponent<RoundedPanel>() == null ||
                fruitGuide.marker.GetComponent<RoundedPanel>().raycastTarget)
                errors.Add("Optional last-fruit guide is not wired or blocks touch input");

            var completion = UnityEngine.Object.FindAnyObjectByType<CalmCompletionFeedback>();
            if (completion == null || completion.orchard != orchard ||
                completion.message == null || completion.messageGroup == null ||
                completion.messageGroup.blocksRaycasts)
                errors.Add("Non-blocking harvest completion feedback is not wired");

            var soundObject = GameObject.Find("Orchard Sounds");
            var sound = soundObject != null ? soundObject.GetComponent<OrchardAudio>() : null;
            if (sound == null || sound.tree != tree || sound.environment != environment ||
                sound.soundStatusText == null)
                errors.Add("Orchard audio has missing references");

            if (!FruitTreeReleaseConfig.AdvertisingEnabled &&
                pause != null && pause.parentPanel != null)
            {
                var purchaseEntry = pause.parentPanel.transform.parent.Find("Parent Access");
                if (purchaseEntry == null || purchaseEntry.gameObject.activeSelf)
                    errors.Add("Ad-free launch must hide the parent purchase entry.");
            }

            if (hudObject != null)
            {
                var buttons = hudObject.GetComponentsInChildren<Button>(true);
                if (buttons.Length < 32)
                    errors.Add("Expected at least 32 functional buttons across home, gameplay, pause and settings");
                foreach (var button in buttons)
                {
                    if (button.GetComponent<ParentHoldGate>() != null) continue;
                    if (button.onClick.GetPersistentEventCount() < 1 ||
                        button.onClick.GetPersistentTarget(0) == null)
                        errors.Add("Button without a persistent callback: " + button.name);
                }
            }

            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VertexLitReference.mat") == null)
                errors.Add("Foliage shader reference material is missing");
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SoftSkyReference.mat") == null)
                errors.Add("Sky shader reference material is missing");

            bool foundBuildScene = false;
            foreach (var entry in EditorBuildSettings.scenes)
                if (entry.enabled && entry.path == path) foundBuildScene = true;
            if (!foundBuildScene) errors.Add("Playable scene is not in build settings");

            if (errors.Count > 0)
                throw new InvalidOperationException("Fruit Tree validation failed:\n" +
                    string.Join("\n", errors));

            FruitReachabilityAudit.Run();

            Debug.Log("Fruit Tree scene validated. Camera, tree, menu, buttons, " +
                "audio, shaders and build settings have valid serialized references.");
        }
    }
}
#endif
