using UnityEngine;

namespace FruitTree
{
    public enum EnvironmentTheme
    {
        Meadow = 0,
        Park = 1,
        CityPark = 2,
        Mountains = 3
    }

    /// <summary>Four compact palettes. Theme names and ordering are stable save values.</summary>
    public static class EnvironmentThemes
    {
        public const int Count = 4;

        public static bool IsValid(int value) => value >= 0 && value < Count;

        public static EnvironmentTheme DefaultForTree(int treeIndex)
        {
            // A new species feels distinct; players can override freely in Settings.
            switch (treeIndex % Count)
            {
                case 1: return EnvironmentTheme.Park;
                case 2: return EnvironmentTheme.Mountains;
                case 3: return EnvironmentTheme.CityPark;
                default: return EnvironmentTheme.Meadow;
            }
        }

        public static string DisplayName(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park: return "PARK";
                case EnvironmentTheme.CityPark: return "CITY PARK";
                case EnvironmentTheme.Mountains: return "MOUNTAINS";
                default: return "MEADOW";
            }
        }

        public static Color GroundTint(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park: return new Color(.91f, 1.03f, .88f);
                case EnvironmentTheme.CityPark: return new Color(.82f, .94f, .79f);
                case EnvironmentTheme.Mountains: return new Color(.83f, .99f, .91f);
                default: return new Color(1.02f, 1.01f, .94f);
            }
        }

        public static Color GrassTint(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park: return new Color(.91f, 1f, .84f);
                case EnvironmentTheme.CityPark: return new Color(.83f, .93f, .78f);
                case EnvironmentTheme.Mountains: return new Color(.85f, .96f, .88f);
                default: return new Color(1f, 1f, .93f);
            }
        }

        public static Color Horizon(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park: return new Color(.91f, .94f, .85f);
                case EnvironmentTheme.CityPark: return new Color(.93f, .85f, .80f);
                case EnvironmentTheme.Mountains: return new Color(.82f, .91f, .94f);
                default: return new Color(.89f, .96f, .94f);
            }
        }

        public static Color Zenith(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park: return new Color(.53f, .74f, .89f);
                case EnvironmentTheme.CityPark: return new Color(.58f, .74f, .87f);
                case EnvironmentTheme.Mountains: return new Color(.48f, .70f, .88f);
                default: return new Color(.48f, .73f, .92f);
            }
        }

        public static Color Sun(EnvironmentTheme theme)
        {
            return theme == EnvironmentTheme.CityPark ?
                new Color(1f, .88f, .78f) : new Color(1f, .96f, .87f);
        }
    }
}
