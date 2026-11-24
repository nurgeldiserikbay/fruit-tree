using UnityEngine;

namespace FruitTree
{
    /// <summary>How the 3D ground and fog meet one theme's 360-degree photo.</summary>
    public readonly struct PhotoScene
    {
        public readonly string Ground;
        public readonly Color GroundTint, Fog;
        public readonly float FogStart, FogEnd, Softness, Mist, GrassLight;
        public readonly bool FadeGroundToPhoto, ShowGrass;

        /// <summary>A small stream runs past the tree in these natural places.</summary>
        public static bool HasBrook(EnvironmentTheme theme)
        {
            return theme == EnvironmentTheme.Meadow || theme == EnvironmentTheme.Park ||
                theme == EnvironmentTheme.Mountains || theme == EnvironmentTheme.JapaneseGarden;
        }

        private PhotoScene(string ground, Color groundTint, Color fog, float fogStart,
            float fogEnd, float softness, float mist, bool fadeGroundToPhoto, bool showGrass,
            float grassLight)
        {
            GrassLight = grassLight;
            Ground = ground;
            GroundTint = groundTint;
            Fog = fog;
            FogStart = fogStart;
            FogEnd = fogEnd;
            Softness = softness;
            Mist = mist;
            FadeGroundToPhoto = fadeGroundToPhoto;
            ShowGrass = showGrass;
        }

        // Open scenes: the ground dissolves into the ground of the photo itself,
        // so water, paths and gardens in the picture stay visible around the tree.
        private static PhotoScene Open(string ground, Color tint, Color fog, bool grass,
            float grassLight = 1f)
        {
            return new PhotoScene(ground, tint, fog, 16f, 33f, .45f, 0f, true, grass, grassLight);
        }

        public static PhotoScene For(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Park:
                    return Open("Grass", new Color(.82f, .99f, .63f), new Color(.56f, .55f, .47f), true);
                case EnvironmentTheme.CityPark:
                    return Open("Grass", new Color(.57f, .70f, .46f), new Color(.60f, .61f, .58f), true, .8f);
                case EnvironmentTheme.Mountains:
                    return Open("Grass", new Color(.74f, .72f, .50f), new Color(.62f, .64f, .63f), true, .72f);
                case EnvironmentTheme.JapaneseGarden:
                    return Open("Gravel", new Color(.73f, .79f, .83f), new Color(.56f, .58f, .50f), false);
                case EnvironmentTheme.ChineseGarden:
                    return Open("Gravel", new Color(.78f, .78f, .78f), new Color(.56f, .58f, .53f), false);
                case EnvironmentTheme.Lakeside:
                    return Open("Shore", new Color(1.1f, 1.05f, .95f), new Color(.72f, .67f, .60f), false);
                case EnvironmentTheme.EveningGarden:
                    return Open("Grass", new Color(.39f, .49f, .41f), new Color(.44f, .39f, .40f), true, .48f);
                default:
                    // Misty forest: the floor fades into the photo's fog instead.
                    return new PhotoScene("MeadowGrass", new Color(1.5f, 1.45f, 1.25f),
                        new Color(.670f, .679f, .650f), 9f, 24f, 1f, 1f, false, true, 1f);
            }
        }
    }
}
