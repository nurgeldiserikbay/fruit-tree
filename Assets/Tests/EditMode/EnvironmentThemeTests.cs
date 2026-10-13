using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class EnvironmentThemeTests
    {
        [Test]
        public void AllEightThemesHaveFiniteSmallStaticGeometry()
        {
            for (int i = 0; i < EnvironmentThemes.Count; i++)
            {
                var theme = (EnvironmentTheme)i;
                Mesh mesh = EnvironmentController.CreateSceneryMesh(theme);
                try
                {
                    Assert.Greater(mesh.vertexCount, 100, theme.ToString());
                    Assert.Less(mesh.vertexCount, 6000, theme.ToString());
                    Assert.Less(mesh.triangles.Length / 3, 2800, theme.ToString());
                    Assert.Greater(mesh.bounds.size.x, 20f, theme.ToString());
                    Assert.Greater(mesh.bounds.size.z, 20f, theme.ToString());
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        Assert.IsFalse(float.IsNaN(vertex.x) || float.IsNaN(vertex.y) ||
                            float.IsNaN(vertex.z), theme.ToString());
                        Assert.IsFalse(float.IsInfinity(vertex.x) || float.IsInfinity(vertex.y) ||
                            float.IsInfinity(vertex.z), theme.ToString());
                        // 360-degree camera and fruit interaction near the central tree stay clear.
                        float radial = new Vector2(vertex.x, vertex.z).magnitude;
                        Assert.Greater(radial, 5.1f, theme.ToString());
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void ThemesUseStableNamesAndDefaultTreeProgression()
        {
            Assert.AreEqual(8, EnvironmentThemes.Count);
            Assert.AreEqual("MEADOW", EnvironmentThemes.DisplayName(EnvironmentTheme.Meadow));
            Assert.AreEqual("CITY PARK", EnvironmentThemes.DisplayName(EnvironmentTheme.CityPark));
            Assert.AreEqual("JAPANESE GARDEN", EnvironmentThemes.DisplayName(EnvironmentTheme.JapaneseGarden));
            Assert.AreEqual("CHINESE GARDEN", EnvironmentThemes.DisplayName(EnvironmentTheme.ChineseGarden));
            Assert.AreEqual("LAKESIDE", EnvironmentThemes.DisplayName(EnvironmentTheme.Lakeside));
            Assert.AreEqual("EVENING GARDEN", EnvironmentThemes.DisplayName(EnvironmentTheme.EveningGarden));
            Assert.AreEqual(EnvironmentTheme.Meadow, EnvironmentThemes.DefaultForTree(0));
            Assert.AreEqual(EnvironmentTheme.Park, EnvironmentThemes.DefaultForTree(1));
            Assert.AreEqual(EnvironmentTheme.Mountains, EnvironmentThemes.DefaultForTree(2));
            Assert.AreEqual(EnvironmentTheme.CityPark, EnvironmentThemes.DefaultForTree(3));
            Assert.AreEqual(EnvironmentTheme.JapaneseGarden, EnvironmentThemes.DefaultForTree(4));
            Assert.AreEqual(EnvironmentTheme.ChineseGarden, EnvironmentThemes.DefaultForTree(5));
            Assert.AreEqual(EnvironmentTheme.Lakeside, EnvironmentThemes.DefaultForTree(6));
            Assert.AreEqual(EnvironmentTheme.EveningGarden, EnvironmentThemes.DefaultForTree(7));
            Assert.IsFalse(EnvironmentThemes.IsValid(-1));
            Assert.IsFalse(EnvironmentThemes.IsValid(8));
        }

        [Test]
        public void SceneColorTransitionEasesSmoothlyWithoutOvershoot()
        {
            Assert.AreEqual(0f, EnvironmentController.TransitionWeight(-2f));
            Assert.AreEqual(0f, EnvironmentController.TransitionWeight(0f));
            Assert.AreEqual(1f, EnvironmentController.TransitionWeight(1f));
            Assert.AreEqual(1f, EnvironmentController.TransitionWeight(4f));
            Assert.Less(EnvironmentController.TransitionWeight(.15f), .15f);
            Assert.Greater(EnvironmentController.TransitionWeight(.85f), .85f);
            float previous = 0f;
            for (int step = 1; step <= 20; step++)
            {
                float weight = EnvironmentController.TransitionWeight(step / 20f);
                Assert.GreaterOrEqual(weight, previous);
                Assert.LessOrEqual(weight, 1f);
                previous = weight;
            }
        }

        [Test]
        public void EveryThemeDefinesValidSkyAndTerrainPalette()
        {
            for (int i = 0; i < EnvironmentThemes.Count; i++)
            {
                var t = (EnvironmentTheme)i;
                Assert.Greater(EnvironmentThemes.GroundTint(t).g, 0f);
                Assert.Greater(EnvironmentThemes.GrassTint(t).g, 0f);
                Assert.Greater(EnvironmentThemes.Horizon(t).b, 0f);
                Assert.Greater(EnvironmentThemes.Zenith(t).b, 0f);
            }
        }
    }
}
