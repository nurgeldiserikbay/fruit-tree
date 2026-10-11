using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Front door and settings for a calm one-scene mobile game.
    /// Existing orchard gameplay remains loaded behind the home screen.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class FruitTreeScreenFlow : MonoBehaviour
    {
        public GameObject homePanel;
        public GameObject gameplayControls;
        public GameObject settingsPanel;
        public GameObject helpPanel;
        public GameObject privacyPanel;
        public PauseController pause;
        public OrchardGameController orchard;
        public OrbitPickingCamera orbitCamera;
        public OrchardAudio sound;
        public Text audioButtonLabel;
        public Text privacyStatusLabel;
        public Button openPublishedPrivacyButton;

        public bool IsPlaying { get; private set; }

        private void Start()
        {
            // Keep the living background visible, but disable world taps until Play.
            IsPlaying = false;
            if (orbitCamera != null) orbitCamera.enabled = false;
            if (gameplayControls != null) gameplayControls.SetActive(false);
            if (homePanel != null) homePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(false);
            if (privacyPanel != null) privacyPanel.SetActive(false);
            if (pause != null) pause.Resume();
            RefreshLabels();
        }

        public void Play()
        {
            IsPlaying = true;
            CloseSubpanels();
            if (homePanel != null) homePanel.SetActive(false);
            if (pause != null) pause.Resume();
            if (gameplayControls != null) gameplayControls.SetActive(true);
            if (orbitCamera != null) orbitCamera.enabled = true;
        }

        public void OpenCollectionFromHome()
        {
            Play();
            if (pause != null) pause.Toggle();
            if (orchard != null) orchard.OpenTreeMenu();
        }

        public void ReturnHome()
        {
            if (pause != null) pause.Resume();
            CloseSubpanels();
            IsPlaying = false;
            if (homePanel != null) homePanel.SetActive(true);
            if (gameplayControls != null) gameplayControls.SetActive(false);
            if (orbitCamera != null) orbitCamera.enabled = false;
            RefreshLabels();
        }

        public void OpenSettingsFromHome()
        {
            OpenSettings();
        }

        public void OpenSettingsFromPause()
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            if (helpPanel != null) helpPanel.SetActive(false);
            if (privacyPanel != null) privacyPanel.SetActive(false);
            RefreshLabels();
        }

        public void CloseSettings()
        {
            if (privacyPanel != null) privacyPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void OpenHelp()
        {
            if (helpPanel != null) helpPanel.SetActive(true);
        }

        public void CloseHelp()
        {
            if (helpPanel != null) helpPanel.SetActive(false);
        }

        public void OpenPrivacy()
        {
            if (privacyPanel != null) privacyPanel.SetActive(true);
            RefreshLabels();
        }

        public void ClosePrivacy()
        {
            if (privacyPanel != null) privacyPanel.SetActive(false);
        }

        public void OpenPublicPrivacyUrl()
        {
            string url = FruitTreePrivacyConfig.PublishedPolicyUrl;
            if (!FruitTreePrivacyConfig.IsPublishedUrlValid(url))
            {
                if (privacyStatusLabel != null)
                    privacyStatusLabel.text = "Online policy will be available before release.";
                return;
            }
            Application.OpenURL(url);
        }

        public void ToggleSound()
        {
            if (sound != null) sound.ToggleSound();
            RefreshLabels();
        }

        public void HandleBackButton()
        {
            if (privacyPanel != null && privacyPanel.activeSelf) { ClosePrivacy(); return; }
            if (helpPanel != null && helpPanel.activeSelf) { CloseHelp(); return; }
            if (settingsPanel != null && settingsPanel.activeSelf) { CloseSettings(); return; }
            if (pause != null && pause.parentPanel != null && pause.parentPanel.activeSelf)
            {
                pause.parentPanel.SetActive(false);
                return;
            }
            if (pause != null && pause.treePanel != null && pause.treePanel.activeSelf)
            {
                if (orchard != null) orchard.CloseTreeMenu();
                return;
            }
            if (!IsPlaying) return; // Android Back never unexpectedly exits the app.
            if (pause != null) pause.Toggle();
        }

        private void CloseSubpanels()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(false);
            if (privacyPanel != null) privacyPanel.SetActive(false);
        }

        private void RefreshLabels()
        {
            if (audioButtonLabel != null && sound != null)
                audioButtonLabel.text = sound.IsMuted ? "SOUND: OFF" : "SOUND: ON";
            bool published = FruitTreePrivacyConfig.IsPublishedUrlValid(
                FruitTreePrivacyConfig.PublishedPolicyUrl);
            if (openPublishedPrivacyButton != null)
                openPublishedPrivacyButton.interactable = published;
            if (privacyStatusLabel != null)
                privacyStatusLabel.text = published ? "Read the full policy online." :
                    "Online policy will be available before release.";
        }
    }
}
