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
        public GameObject parentPanel;
        public bool IsPaused { get; private set; }

        private void Update()
        {
            // Android back button maps to Escape in the legacy input system.
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }

        public void Toggle() { SetPaused(!IsPaused); }
        public void Resume() { SetPaused(false); }

        public void Restart()
        {
            if (hud != null) hud.Restart();
            Resume();
        }

        private void SetPaused(bool paused)
        {
            if (panel == null) return;
            IsPaused = paused;
            if (!paused && parentPanel != null) parentPanel.SetActive(false);
            panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        private void OnApplicationPause(bool appPaused)
        {
            // A call, notification, or app switch should not let the picking round
            // continue silently in the background.
            if (appPaused && !IsPaused) SetPaused(true);
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
