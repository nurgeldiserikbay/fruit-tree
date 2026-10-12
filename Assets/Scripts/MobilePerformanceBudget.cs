using UnityEngine;

namespace FruitTree
{
    public enum MobilePerformanceTier
    {
        Low,
        Balanced,
        High
    }

    /// <summary>
    /// Lightweight mobile frame-budget defaults. Never uses a continuous
    /// FPS polling loop and never changes fruit count or save identities.
    /// The platform limits remain targets until measured on actual phones.
    /// </summary>
    public sealed class MobilePerformanceBudget : MonoBehaviour
    {
        public Light sun;
        public MobilePerformanceTier ActiveTier { get; private set; }

        public static MobilePerformanceTier Recommend(int systemRamMb, int graphicsRamMb)
        {
            if ((systemRamMb > 0 && systemRamMb < 3000) ||
                (graphicsRamMb > 0 && graphicsRamMb < 512))
                return MobilePerformanceTier.Low;
            if ((systemRamMb > 0 && systemRamMb < 6000) ||
                (graphicsRamMb > 0 && graphicsRamMb < 1500))
                return MobilePerformanceTier.Balanced;
            return MobilePerformanceTier.High;
        }

        private void Start()
        {
            if (!Application.isMobilePlatform) return;
            ActiveTier = Recommend(SystemInfo.systemMemorySize,
                SystemInfo.graphicsMemorySize);
            QualitySettings.vSyncCount = 0;
            switch (ActiveTier)
            {
                case MobilePerformanceTier.Low:
                    Application.targetFrameRate = 30;
                    QualitySettings.shadowDistance = 0f;
                    QualitySettings.antiAliasing = 0;
                    if (sun != null) sun.shadows = LightShadows.None;
                    break;
                case MobilePerformanceTier.Balanced:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadowDistance = 15f;
                    QualitySettings.antiAliasing = 2;
                    if (sun != null) sun.shadows = LightShadows.Hard;
                    break;
                default:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadowDistance = 23f;
                    QualitySettings.antiAliasing = 2;
                    if (sun != null) sun.shadows = LightShadows.Soft;
                    break;
            }
            Debug.Log("Fruit Tree mobile visual tier: " + ActiveTier +
                " (RAM=" + SystemInfo.systemMemorySize +
                "MB, VRAM=" + SystemInfo.graphicsMemorySize + "MB)");
        }
    }
}
