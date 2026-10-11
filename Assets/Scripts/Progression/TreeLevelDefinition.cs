using System;

namespace FruitTree.Progression
{
    /// <summary>Stable rules-level data. Rendering/prefab references live elsewhere.</summary>
    public sealed class TreeLevelDefinition
    {
        public string Id { get; }
        public string SpeciesId { get; }
        public int FruitCount { get; }

        public TreeLevelDefinition(string id, string speciesId, int fruitCount)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Tree ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(speciesId)) throw new ArgumentException("Species ID is required.", nameof(speciesId));
            if (fruitCount < 1 || fruitCount > 2000) throw new ArgumentOutOfRangeException(nameof(fruitCount));
            Id = id;
            SpeciesId = speciesId;
            FruitCount = fruitCount;
        }

        public string FruitIdAt(int index)
        {
            if (index < 0 || index >= FruitCount) throw new ArgumentOutOfRangeException(nameof(index));
            return "fruit_" + index.ToString("D4");
        }

        public bool ContainsFruitId(string fruitId)
        {
            if (string.IsNullOrEmpty(fruitId) || !fruitId.StartsWith("fruit_", StringComparison.Ordinal)
                || fruitId.Length != 10) return false;
            if (!int.TryParse(fruitId.Substring(6), out int index)) return false;
            return index >= 0 && index < FruitCount && FruitIdAt(index) == fruitId;
        }
    }
}
