using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class SoundTests
    {
        [Test]
        public void VolumeSlidersGoFromSilenceToALittleLouder()
        {
            Assert.AreEqual(0f, AudioLevels.GainFor(0f));
            Assert.That(AudioLevels.GainFor(AudioLevels.Default), Is.EqualTo(1f).Within(.001f),
                "The default level keeps every sound at its designed loudness.");
            Assert.That(AudioLevels.GainFor(1f), Is.InRange(1.4f, 1.7f));
            Assert.Less(AudioLevels.GainFor(.4f), AudioLevels.GainFor(.6f));
        }

        [Test]
        public void PickSoundsVaryAndNeverClip()
        {
            float[] previous = null;
            for (int variant = 0; variant < 4; variant++)
            {
                AudioClip clip = OrchardAudio.CreatePick(variant);
                try
                {
                    var samples = new float[clip.samples];
                    clip.GetData(samples, 0);
                    foreach (float sample in samples) Assert.That(sample, Is.InRange(-.85f, .85f));
                    if (previous != null) CollectionAssert.AreNotEqual(previous, samples);
                    previous = samples;
                }
                finally { Object.DestroyImmediate(clip); }
            }
            Assert.Greater(OrchardAudio.PickPitch("cherry"), OrchardAudio.PickPitch("orange"),
                "Small fruit sounds higher than big fruit.");
        }

        [Test]
        public void LeavesRustleOnlyWhenMovingThroughThem()
        {
            Assert.AreEqual(0f, FoliageRustle.Intensity(3f, 0f, 0f), "Away from the leaves: silent.");
            Assert.AreEqual(0f, FoliageRustle.Intensity(0f, 0f, 1f), "Still in the leaves: silent.");
            Assert.Greater(FoliageRustle.Intensity(1f, 0f, 1f), 0f);
            Assert.LessOrEqual(FoliageRustle.Intensity(50f, 500f, 1f), 1f);
            AudioClip rustle = FoliageRustle.CreateRustle();
            try
            {
                var samples = new float[rustle.samples];
                rustle.GetData(samples, 0);
                foreach (float sample in samples) Assert.That(sample, Is.InRange(-.8f, .8f));
            }
            finally { Object.DestroyImmediate(rustle); }
        }

        [Test]
        public void EveryNewTreeGetsDifferentWeather()
        {
            foreach (WeatherKind current in System.Enum.GetValues(typeof(WeatherKind)))
                for (int step = 0; step < 100; step++)
                    Assert.AreNotEqual(current, WeatherController.PickForNewTree(current, step / 100.0));
        }
    }
}
