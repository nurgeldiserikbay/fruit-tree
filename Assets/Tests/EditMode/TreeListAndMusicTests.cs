using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class TreeListAndMusicTests
    {
        [Test]
        public void TreeListDescribesEveryState()
        {
            var entry = new TreeCollectionScreen.Entry { Picked = 37, Total = 175, Unlocked = false };
            Assert.AreEqual("LOCKED", TreeCollectionScreen.StatusText(entry));
            entry.Unlocked = true;
            Assert.AreEqual("37 / 175", TreeCollectionScreen.StatusText(entry));
            entry.Active = true;
            StringAssert.StartsWith("37 / 175", TreeCollectionScreen.StatusText(entry));
            entry.Cleared = true;
            StringAssert.Contains("REGROW", TreeCollectionScreen.StatusText(entry));
        }

        [Test]
        public void EachTreeGetsItsOwnTrackAndWrapsAround()
        {
            Assert.AreEqual(0, OrchardAudio.TrackForTree(0, 6));
            Assert.AreEqual(5, OrchardAudio.TrackForTree(5, 6));
            Assert.AreEqual(0, OrchardAudio.TrackForTree(6, 6));
            Assert.AreEqual(3, OrchardAudio.TrackForTree(9, 6));
            Assert.AreEqual(0, OrchardAudio.TrackForTree(4, 0));
        }

        [Test]
        public void SpeciesHaveDistinctCrownHabits()
        {
            var pear = FruitSpeciesProfile.Get("pear");
            var cherry = FruitSpeciesProfile.Get("cherry");
            var orange = FruitSpeciesProfile.Get("orange");
            Assert.Less(pear.PhotoReach, cherry.PhotoReach, "Pear is narrow, cherry is wide.");
            Assert.Greater(pear.PhotoLimbRise, cherry.PhotoLimbRise, "Pear climbs, cherry spreads.");
            Assert.Less(orange.PhotoCrownBase, pear.PhotoCrownBase, "Citrus crowns start low.");
            Assert.AreEqual(3, FruitSpeciesProfile.Get("pomegranate").PhotoStems);
            foreach (var level in OrchardGameController.LevelDefinitions)
                Assert.IsNotNull(Resources.Load<Texture2D>("TreeTextures/Leaves/" + level.SpeciesId),
                    level.SpeciesId + " needs its own leaf shape");
        }
    }
}
