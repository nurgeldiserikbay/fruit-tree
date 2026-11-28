using System;
using System.Collections.Generic;

namespace FruitTree.Garden
{
    /// <summary>One bed in the player's garden, as saved on the device.</summary>
    [Serializable]
    public sealed class GardenPlotData
    {
        public string speciesId = "";
        /// <summary>UTC ticks when the seed went into the ground (or the last harvest ended).</summary>
        public long plantedTicks;
        /// <summary>Extra growth earned by watering, in seconds.</summary>
        public long bonusSeconds;
        public long lastWateredTicks;
        /// <summary>True once the fruit of the current round has all been picked.</summary>
        public bool harvested;
        public List<string> pickedFruitIds = new List<string>();
    }

    [Serializable]
    public sealed class GardenSeedData
    {
        public string speciesId = "";
        public int count;
    }

    [Serializable]
    public sealed class GardenSaveData
    {
        public int schemaVersion = 1;
        public List<GardenSeedData> seeds = new List<GardenSeedData>();
        public List<GardenPlotData> plots = new List<GardenPlotData>();
    }

    public enum GardenStage { Empty, Sprout, Sapling, YoungTree, Blossom, Fruit, Resting }

    /// <summary>
    /// The calm garden: seeds come from fully picked orchard trees, a planted
    /// seed grows by itself in real time and never wilts or dies. Watering is
    /// optional and only makes it grow a little sooner. Ripe fruit can be picked;
    /// afterwards the tree rests and fruits again later. Pure logic, no Unity.
    /// </summary>
    public sealed class GardenState
    {
        public const int PlotCount = 6;
        public const int FruitPerTree = 60;
        public static readonly TimeSpan GrowTime = TimeSpan.FromHours(36);
        public static readonly TimeSpan RestTime = TimeSpan.FromHours(20);
        public static readonly TimeSpan WaterBonus = TimeSpan.FromHours(3);
        public static readonly TimeSpan WaterPause = TimeSpan.FromHours(1);

        private readonly GardenSaveData data;

        public GardenState(GardenSaveData save)
        {
            data = save ?? new GardenSaveData();
            if (data.seeds == null) data.seeds = new List<GardenSeedData>();
            if (data.plots == null) data.plots = new List<GardenPlotData>();
            while (data.plots.Count < PlotCount) data.plots.Add(new GardenPlotData());
            foreach (GardenPlotData plot in data.plots)
            {
                if (plot.speciesId == null) plot.speciesId = "";
                if (plot.pickedFruitIds == null) plot.pickedFruitIds = new List<string>();
                if (plot.bonusSeconds < 0) plot.bonusSeconds = 0;
            }
        }

        public GardenSaveData Data => data;
        public GardenPlotData Plot(int index) => data.plots[index];

        // ---------- Seeds ----------

        public int SeedCount(string species)
        {
            foreach (GardenSeedData seed in data.seeds)
                if (seed.speciesId == species) return seed.count;
            return 0;
        }

        public int TotalSeeds
        {
            get
            {
                int total = 0;
                foreach (GardenSeedData seed in data.seeds) total += Math.Max(0, seed.count);
                return total;
            }
        }

        /// <summary>Species with at least one seed, in the order they were first earned.</summary>
        public List<string> SeedSpecies()
        {
            var list = new List<string>();
            foreach (GardenSeedData seed in data.seeds)
                if (seed.count > 0) list.Add(seed.speciesId);
            return list;
        }

        public void AddSeed(string species, int count = 1)
        {
            if (string.IsNullOrEmpty(species) || count <= 0) return;
            foreach (GardenSeedData seed in data.seeds)
            {
                if (seed.speciesId != species) continue;
                seed.count += count;
                return;
            }
            data.seeds.Add(new GardenSeedData { speciesId = species, count = count });
        }

        // ---------- Planting, growing, watering ----------

        public bool TryPlant(int plot, string species, DateTime nowUtc)
        {
            if (!ValidPlot(plot) || !string.IsNullOrEmpty(Plot(plot).speciesId)) return false;
            GardenSeedData seed = data.seeds.Find(s => s.speciesId == species && s.count > 0);
            if (seed == null) return false;
            seed.count--;
            GardenPlotData bed = Plot(plot);
            bed.speciesId = species;
            bed.plantedTicks = nowUtc.Ticks;
            bed.bonusSeconds = 0;
            bed.lastWateredTicks = 0;
            bed.harvested = false;
            bed.pickedFruitIds.Clear();
            return true;
        }

        /// <summary>0 at planting, 1 when the first fruit is ripe. Never goes back.</summary>
        public double Growth(int plot, DateTime nowUtc)
        {
            GardenPlotData bed = Plot(plot);
            if (string.IsNullOrEmpty(bed.speciesId)) return 0;
            return Math.Min(1.0, Elapsed(bed, nowUtc).TotalSeconds / GrowTime.TotalSeconds);
        }

        public GardenStage Stage(int plot, DateTime nowUtc)
        {
            GardenPlotData bed = Plot(plot);
            if (string.IsNullOrEmpty(bed.speciesId)) return GardenStage.Empty;
            if (bed.harvested) return GardenStage.Resting;
            double growth = Growth(plot, nowUtc);
            if (growth >= 1.0) return GardenStage.Fruit;
            if (growth >= .85) return GardenStage.Blossom;
            if (growth >= .5) return GardenStage.YoungTree;
            if (growth >= .2) return GardenStage.Sapling;
            return GardenStage.Sprout;
        }

        /// <summary>Time until the next fruit; zero when ripe or the bed is empty.</summary>
        public TimeSpan TimeToFruit(int plot, DateTime nowUtc)
        {
            GardenPlotData bed = Plot(plot);
            if (string.IsNullOrEmpty(bed.speciesId)) return TimeSpan.Zero;
            TimeSpan goal = bed.harvested ? RestTime : GrowTime;
            TimeSpan left = goal - Elapsed(bed, nowUtc);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        /// <summary>The visible size of the tree: a small sprout grows to full size.</summary>
        public float TreeScale(int plot, DateTime nowUtc)
        {
            GardenPlotData bed = Plot(plot);
            if (string.IsNullOrEmpty(bed.speciesId)) return 0f;
            if (bed.harvested) return 1f;
            return (float)(.16 + .84 * Growth(plot, nowUtc));
        }

        public bool CanWater(int plot, DateTime nowUtc)
        {
            if (!ValidPlot(plot)) return false;
            GardenStage stage = Stage(plot, nowUtc);
            if (stage == GardenStage.Empty || stage == GardenStage.Fruit) return false;
            long last = Plot(plot).lastWateredTicks;
            return last == 0 || nowUtc.Ticks < last || nowUtc - new DateTime(last, DateTimeKind.Utc) >= WaterPause;
        }

        /// <summary>Optional care: a little faster growth. The soil then stays moist for a while.</summary>
        public bool TryWater(int plot, DateTime nowUtc)
        {
            if (!CanWater(plot, nowUtc)) return false;
            GardenPlotData bed = Plot(plot);
            bed.bonusSeconds += (long)WaterBonus.TotalSeconds;
            bed.lastWateredTicks = nowUtc.Ticks;
            return true;
        }

        /// <summary>Time until watering helps again.</summary>
        public TimeSpan SoilMoistFor(int plot, DateTime nowUtc)
        {
            long last = Plot(plot).lastWateredTicks;
            if (last == 0 || nowUtc.Ticks < last) return TimeSpan.Zero;
            TimeSpan left = WaterPause - (nowUtc - new DateTime(last, DateTimeKind.Utc));
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        // ---------- Harvest ----------

        /// <summary>Records one picked garden fruit. True when the whole tree is now picked.</summary>
        public bool PickFruit(int plot, string fruitId, DateTime nowUtc)
        {
            if (Stage(plot, nowUtc) != GardenStage.Fruit || string.IsNullOrEmpty(fruitId)) return false;
            GardenPlotData bed = Plot(plot);
            if (!bed.pickedFruitIds.Contains(fruitId)) bed.pickedFruitIds.Add(fruitId);
            if (bed.pickedFruitIds.Count < FruitPerTree) return false;
            // All picked: the tree rests, then fruits again. It never wilts.
            bed.harvested = true;
            bed.plantedTicks = nowUtc.Ticks;
            bed.bonusSeconds = 0;
            bed.lastWateredTicks = 0;
            bed.pickedFruitIds.Clear();
            return true;
        }

        /// <summary>A resting tree that has waited long enough carries new fruit.</summary>
        public bool RefreshResting(int plot, DateTime nowUtc)
        {
            GardenPlotData bed = Plot(plot);
            if (!bed.harvested || string.IsNullOrEmpty(bed.speciesId)) return false;
            if (Elapsed(bed, nowUtc) < RestTime) return false;
            bed.harvested = false;
            // Planted far enough back that the tree counts as fully grown again.
            bed.plantedTicks = (nowUtc - GrowTime).Ticks;
            bed.bonusSeconds = 0;
            bed.pickedFruitIds.Clear();
            return true;
        }

        public bool RefreshAll(DateTime nowUtc)
        {
            bool changed = false;
            for (int i = 0; i < PlotCount; i++) changed |= RefreshResting(i, nowUtc);
            return changed;
        }

        private static TimeSpan Elapsed(GardenPlotData bed, DateTime nowUtc)
        {
            // A clock set backwards never makes a tree shrink.
            long ticks = nowUtc.Ticks - bed.plantedTicks;
            if (ticks < 0) ticks = 0;
            return TimeSpan.FromTicks(ticks) + TimeSpan.FromSeconds(Math.Max(0, bed.bonusSeconds));
        }

        private static bool ValidPlot(int plot) => plot >= 0 && plot < PlotCount;

        /// <summary>"about 3 h", "about 25 min", "a moment".</summary>
        public static string Describe(TimeSpan span)
        {
            if (span.TotalMinutes < 1) return "a moment";
            if (span.TotalHours < 1) return "about " + Math.Max(1, (int)Math.Ceiling(span.TotalMinutes)) + " min";
            if (span.TotalHours < 36) return "about " + (int)Math.Ceiling(span.TotalHours) + " h";
            return "about " + (int)Math.Round(span.TotalDays) + " days";
        }
    }
}
