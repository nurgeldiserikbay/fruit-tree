using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// A deliberately small pause system. The pause overlay contains the
    /// secondary controls, leaving the apple tree unobstructed during play.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        public GameObject panel;
        public HarvestHUD hud;
        public OrchardGameController orchard;
        public GameObject parentPanel;
        public GameObject treePanel;
        public FruitTreeScreenFlow screenFlow;
        public bool IsPaused { get; private set; }

        private void Update()
        {
            // Android back button maps to Escape in the legacy input system.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (screenFlow != null) screenFlow.HandleBackButton();
                else Toggle();
            }
        }

        public void Toggle() { SetPaused(!IsPaused); }
        public void Resume() { SetPaused(false); }

        public void Restart()
        {
            if (hud != null && hud.OrchardMode)
            {
                // Only completed trees may restart, via the persisted round
                // transition. A partial tree must not silently lose progress.
                if (orchard != null && orchard.CanRebloomActive())
                    orchard.Rebloom();
                return;
            }
            if (hud != null) hud.Restart();
            Resume();
        }

        private void SetPaused(bool paused)
        {
            if (panel == null) return;
            IsPaused = paused;
            if (!paused && parentPanel != null) parentPanel.SetActive(false);
            if (!paused && treePanel != null) treePanel.SetActive(false);
            panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        private void OnApplicationPause(bool appPaused)
        {
            // A call, notification, or app switch should not let the picking round
            // continue silently in the background.
            if (appPaused && !IsPaused && (screenFlow == null || screenFlow.IsPlaying))
                SetPaused(true);
        }

        private void OnDisable()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
