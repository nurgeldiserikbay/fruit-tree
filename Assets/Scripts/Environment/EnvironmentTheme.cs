using UnityEngine;

namespace FruitTree
{
    // Never renumber: integers 0-3 are persisted in existing player preferences.
    public enum EnvironmentTheme
    {
        Meadow = 0,
        Park = 1,
        CityPark = 2,
        Mountains = 3,
        JapaneseGarden = 4,
        ChineseGarden = 5,
        Lakeside = 6,
        EveningGarden = 7
    }

    /// <summary>Stable IDs, default progression, and affordable per-theme colors.</summary>
    public static class EnvironmentThemes
    {
        public const int Count = 8;

        public static bool IsValid(int value) => value >= 0 && value < Count;

        public static EnvironmentTheme DefaultForTree(int treeIndex)
        {
            // Keep trees 1-4 unchanged for existing users; new environments
            // arrive naturally from tree 5 onward. Settings can override them.
            switch (Mathf.Max(0, treeIndex) % Count)
            {
                case 1: return EnvironmentTheme.Park;
                case 2: return EnvironmentTheme.Mountains;
                case 3: return EnvironmentTheme.CityPark;
                case 4: return EnvironmentTheme.JapaneseGarden;
                case 5: return EnvironmentTheme.ChineseGarden;
                case 6: return EnvironmentTheme.Lakeside;
                case 7: return EnvironmentTheme.EveningGarden;
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
                case EnvironmentTheme.JapaneseGarden: return "JAPANESE GARDEN";
                case EnvironmentTheme.ChineseGarden: return "CHINESE GARDEN";
                case EnvironmentTheme.Lakeside: return "LAKESIDE";
                case EnvironmentTheme.EveningGarden: return "EVENING GARDEN";
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
                case EnvironmentTheme.JapaneseGarden: return new Color(.91f, .99f, .93f);
                case EnvironmentTheme.ChineseGarden: return new Color(.99f, .96f, .89f);
                case EnvironmentTheme.Lakeside: return new Color(.84f, 1.01f, .94f);
                case EnvironmentTheme.EveningGarden: return new Color(.94f, .85f, .80f);
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
                case EnvironmentTheme.JapaneseGarden: return new Color(.84f, .98f, .88f);
                case EnvironmentTheme.ChineseGarden: return new Color(.93f, .97f, .78f);
                case EnvironmentTheme.Lakeside: return new Color(.74f, .93f, .88f);
                case EnvironmentTheme.EveningGarden: return new Color(.89f, .83f, .72f);
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
                case EnvironmentTheme.JapaneseGarden: return new Color(.93f, .94f, .90f);
                case EnvironmentTheme.ChineseGarden: return new Color(.98f, .89f, .79f);
                case EnvironmentTheme.Lakeside: return new Color(.85f, .95f, .96f);
                case EnvironmentTheme.EveningGarden: return new Color(.98f, .63f, .60f);
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
                case EnvironmentTheme.JapaneseGarden: return new Color(.61f, .77f, .88f);
                case EnvironmentTheme.ChineseGarden: return new Color(.65f, .70f, .86f);
                case EnvironmentTheme.Lakeside: return new Color(.52f, .78f, .92f);
                case EnvironmentTheme.EveningGarden: return new Color(.35f, .40f, .66f);
                default: return new Color(.48f, .73f, .92f);
            }
        }

        public static Color Sun(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.CityPark: return new Color(1f, .88f, .78f);
                case EnvironmentTheme.EveningGarden: return new Color(1f, .70f, .59f);
                case EnvironmentTheme.ChineseGarden: return new Color(1f, .87f, .76f);
                default: return new Color(1f, .96f, .87f);
            }
        }
    }
}
