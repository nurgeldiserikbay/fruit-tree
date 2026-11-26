using System;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// GRAPHICS: HIGH / LOW. Low turns off the crown shadow, falling leaves and
    /// wildlife and grows trees with 30% fewer leaves, for slower phones.
    /// </summary>
    public static class GraphicsQuality
    {
        private const string Key = "FruitTree.Graphics";
        private static int cached = -1;
        public static event Action Changed;

        /// <summary>Phones with little memory start on Low.</summary>
        public static bool DefaultHigh => SystemInfo.systemMemorySize <= 0 || SystemInfo.systemMemorySize >= 3000;

        public static bool High
        {
            get
            {
                if (cached < 0) cached = PlayerPrefs.GetInt(Key, DefaultHigh ? 1 : 0);
                return cached == 1;
            }
        }

        public static float LeafDensity => High ? 1f : .7f;

        public static void Set(bool high)
        {
            if (high == High) return;
            cached = high ? 1 : 0;
            PlayerPrefs.SetInt(Key, cached);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Toggle() => Set(!High);
    }
}
