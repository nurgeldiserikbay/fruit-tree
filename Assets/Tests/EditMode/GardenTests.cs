using System;
using FruitTree.Garden;
using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class GardenTests
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 28, 9, 0, 0, DateTimeKind.Utc);

        private static GardenState WithSeed(string species = "apple")
        {
            var garden = new GardenState(new GardenSaveData());
            garden.AddSeed(species);
            return garden;
        }

        [Test]
        public void PlantingUsesOneSeedAndOnlyAnEmptyBed()
        {
            var garden = WithSeed();
            garden.AddSeed("apple");
            Assert.AreEqual(GardenState.PlotCount, garden.Data.plots.Count);
            Assert.IsTrue(garden.TryPlant(0, "apple", Start));
            Assert.AreEqual(1, garden.SeedCount("apple"));
            Assert.IsFalse(garden.TryPlant(0, "apple", Start), "The bed is taken.");
            Assert.IsFalse(garden.TryPlant(1, "cherry", Start), "No cherry seed.");
            Assert.AreEqual(GardenStage.Sprout, garden.Stage(0, Start));
        }

        [Test]
        public void TreeGrowsByItselfAndNeverWilts()
        {
            var garden = WithSeed();
            garden.TryPlant(0, "apple", Start);
            float previous = 0f;
            for (int hour = 0; hour <= 24 * 30; hour += 3)
            {
                float scale = garden.TreeScale(0, Start.AddHours(hour));
                Assert.GreaterOrEqual(scale, previous, "A tree never shrinks.");
                previous = scale;
            }
            Assert.AreEqual(GardenStage.Fruit, garden.Stage(0, Start + GardenState.GrowTime));
            Assert.AreEqual(GardenStage.Fruit, garden.Stage(0, Start.AddDays(60)),
                "Weeks without a visit: still a healthy tree with fruit.");
        }

        [Test]
        public void WateringHelpsButIsNeverNeeded()
        {
            var watered = WithSeed();
            var left = WithSeed();
            watered.TryPlant(0, "apple", Start);
            left.TryPlant(0, "apple", Start);
            Assert.IsTrue(watered.TryWater(0, Start.AddHours(1)));
            Assert.IsFalse(watered.TryWater(0, Start.AddHours(1.5)), "The soil is still moist.");
            Assert.Greater(watered.Growth(0, Start.AddHours(2)), left.Growth(0, Start.AddHours(2)));
            Assert.AreEqual(GardenStage.Fruit, left.Stage(0, Start + GardenState.GrowTime));
        }

        [Test]
        public void ClockSetBackwardsNeverShrinksTheTree()
        {
            var garden = WithSeed();
            garden.TryPlant(0, "apple", Start);
            Assert.AreEqual(GardenStage.Sprout, garden.Stage(0, Start.AddDays(-3)));
            Assert.That(garden.TreeScale(0, Start.AddDays(-3)), Is.InRange(.1f, .2f));
        }

        [Test]
        public void PickedTreeRestsThenFruitsAgain()
        {
            var garden = WithSeed();
            garden.TryPlant(0, "apple", Start);
            DateTime ripe = Start + GardenState.GrowTime;
            for (int i = 0; i < GardenState.FruitPerTree - 1; i++)
                Assert.IsFalse(garden.PickFruit(0, "fruit_" + i.ToString("D4"), ripe));
            Assert.IsFalse(garden.PickFruit(0, "fruit_0000", ripe), "The same fruit counts once.");
            Assert.IsTrue(garden.PickFruit(0, "fruit_" + (GardenState.FruitPerTree - 1).ToString("D4"), ripe));
            Assert.AreEqual(GardenStage.Resting, garden.Stage(0, ripe));
            Assert.AreEqual(1f, garden.TreeScale(0, ripe), "A resting tree keeps its full size.");
            Assert.IsFalse(garden.RefreshResting(0, ripe.AddHours(1)));
            Assert.IsTrue(garden.RefreshResting(0, ripe + GardenState.RestTime));
            Assert.AreEqual(GardenStage.Fruit, garden.Stage(0, ripe + GardenState.RestTime));
            Assert.AreEqual(0, garden.Plot(0).pickedFruitIds.Count);
        }

        [Test]
        public void GardenSaveSurvivesJson()
        {
            var garden = WithSeed("cherry");
            garden.TryPlant(2, "cherry", Start);
            garden.TryWater(2, Start.AddHours(2));
            string json = JsonUtility.ToJson(garden.Data);
            var loaded = new GardenState(JsonUtility.FromJson<GardenSaveData>(json));
            Assert.AreEqual("cherry", loaded.Plot(2).speciesId);
            Assert.AreEqual(garden.Growth(2, Start.AddHours(5)), loaded.Growth(2, Start.AddHours(5)), 1e-9);
        }

        [Test]
        public void WaitingTimeReadsNaturally()
        {
            Assert.AreEqual("about 3 h", GardenState.Describe(TimeSpan.FromHours(2.2)));
            Assert.AreEqual("about 25 min", GardenState.Describe(TimeSpan.FromMinutes(24.5)));
            Assert.AreEqual("a moment", GardenState.Describe(TimeSpan.FromSeconds(20)));
        }

        [Test]
        public void WateringSoundStaysSoft()
        {
            AudioClip clip = GardenController.CreatePour();
            try
            {
                var samples = new float[clip.samples];
                clip.GetData(samples, 0);
                foreach (float sample in samples) Assert.That(sample, Is.InRange(-.8f, .8f));
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }
    }
}
