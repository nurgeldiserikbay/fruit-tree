using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Small performance sampler for Unity Editor and development APKs.
    /// Silent in production builds; never creates an in-game debug overlay.
    /// </summary>
    public sealed class DevelopmentPerformanceMonitor : MonoBehaviour
    {
        private const float SampleSeconds = 5f;
        private void Awake()
        {
            // Profile only in Editor and development builds (Unity 6.6 compatible).
            enabled = Debug.isDebugBuild;
        }

        private int frames;
        private float seconds;
        private float slowestFrame;
        private int interval;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > .5f) return; // ignore app resume stalls
            seconds += dt;
            slowestFrame = Mathf.Max(slowestFrame, dt);
            frames++;
            if (seconds < SampleSeconds) return;
            float fps = frames / seconds;
            float usedMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            string report = "[FruitTree] fps=" + fps.ToString("F1") +
                " worst_frame_ms=" + (slowestFrame * 1000f).ToString("F1") +
                " managed_and_native_allocated_mb=" + usedMb.ToString("F1");
            if (fps < 28f && interval > 1) Debug.LogWarning(report);
            else Debug.Log(report);
            interval++;
            frames = 0;
            seconds = 0f;
            slowestFrame = 0f;
        }
    }
}
