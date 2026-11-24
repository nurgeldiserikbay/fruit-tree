#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>Switches weather instantly while testing in Play Mode.</summary>
    public static class WeatherMenu
    {
        [MenuItem("Fruit Tree/Weather/Clear")] private static void Clear() => Show(WeatherKind.Clear);
        [MenuItem("Fruit Tree/Weather/Mist")] private static void Mist() => Show(WeatherKind.Mist);
        [MenuItem("Fruit Tree/Weather/Cloudy")] private static void Cloudy() => Show(WeatherKind.Cloudy);
        [MenuItem("Fruit Tree/Weather/Rain")] private static void Rain() => Show(WeatherKind.Rain);
        [MenuItem("Fruit Tree/Weather/Storm")] private static void Storm() => Show(WeatherKind.Storm);

        [MenuItem("Fruit Tree/Weather/Clear", true)]
        [MenuItem("Fruit Tree/Weather/Mist", true)]
        [MenuItem("Fruit Tree/Weather/Cloudy", true)]
        [MenuItem("Fruit Tree/Weather/Rain", true)]
        [MenuItem("Fruit Tree/Weather/Storm", true)]
        private static bool CanShow() => Application.isPlaying;

        private static void Show(WeatherKind kind)
        {
            var weather = Object.FindFirstObjectByType<WeatherController>();
            if (weather != null) weather.ForceWeather(kind);
        }
    }
}
#endif
