using UnityEngine;

namespace FruitTree
{
    public sealed class MobileBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000 ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
