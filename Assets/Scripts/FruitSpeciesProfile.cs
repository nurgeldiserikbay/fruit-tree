using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Immutable per-species visual data. Stable species IDs are save keys;
    /// these palettes can change without affecting previously picked fruit IDs.
    /// </summary>
    public sealed class FruitSpeciesProfile
    {
        public string Id { get; private set; }
        public int BranchCount { get; private set; }
        public float CrownSpread { get; private set; }
        public float CrownHeightLow { get; private set; }
        public float CrownHeightHigh { get; private set; }
        public Color TrunkColor { get; private set; }
        public Color BranchColor { get; private set; }
        public Color LeafColor { get; private set; }
        public float LeafLengthLow { get; private set; }
        public float LeafLengthHigh { get; private set; }
        public float LeafWidthLow { get; private set; }
        public float LeafWidthHigh { get; private set; }
        public float LeafOvalness { get; private set; }
        public Color FruitColor { get; private set; }
        public float FruitScaleLow { get; private set; }
        public float FruitScaleHigh { get; private set; }
        public float HitRadius { get; private set; }

        private FruitSpeciesProfile(string id, int branches, float spread,
            float low, float high, Color trunk, Color wood, Color foliage,
            float leafLow, float leafHigh, float widthLow, float widthHigh,
            float ovalness, Color fruit, float scaleLow, float scaleHigh, float hitRadius)
        {
            Id = id;
            BranchCount = branches;
            CrownSpread = spread;
            CrownHeightLow = low;
            CrownHeightHigh = high;
            TrunkColor = trunk;
            BranchColor = wood;
            LeafColor = foliage;
            LeafLengthLow = leafLow;
            LeafLengthHigh = leafHigh;
            LeafWidthLow = widthLow;
            LeafWidthHigh = widthHigh;
            LeafOvalness = ovalness;
            FruitColor = fruit;
            FruitScaleLow = scaleLow;
            FruitScaleHigh = scaleHigh;
            HitRadius = hitRadius;
        }

        private static readonly Dictionary<string, FruitSpeciesProfile> All =
            new Dictionary<string, FruitSpeciesProfile>(StringComparer.Ordinal)
            {
                { "apple", new FruitSpeciesProfile("apple", 19, 1f, 1.36f, 3.55f,
                    new Color(.78f,.73f,.67f), new Color(.83f,.76f,.67f),
                    new Color(.33f,.59f,.19f), .21f,.32f,.16f,.22f,.58f,
                    Color.white,.54f,.64f,.27f) },
                { "pear", new FruitSpeciesProfile("pear", 17, .79f, 1.47f, 3.74f,
                    new Color(.72f,.71f,.65f), new Color(.77f,.73f,.66f),
                    new Color(.31f,.52f,.18f), .21f,.32f,.11f,.16f,.93f,
                    new Color(.82f,.78f,.23f),.70f,.80f,.27f) },
                { "cherry", new FruitSpeciesProfile("cherry", 23, .92f, 1.22f, 3.32f,
                    new Color(.69f,.60f,.57f), new Color(.73f,.63f,.60f),
                    new Color(.27f,.53f,.18f), .17f,.26f,.16f,.22f,.12f,
                    new Color(.74f,.045f,.065f),.43f,.53f,.24f) },
                { "peach", new FruitSpeciesProfile("peach", 21, 1.05f, 1.30f, 3.38f,
                    new Color(.76f,.66f,.60f), new Color(.81f,.68f,.62f),
                    new Color(.30f,.57f,.22f), .17f,.26f,.16f,.22f,.12f,
                    new Color(.97f,.77f,.67f),.65f,.75f,.28f) },
                { "orange", new FruitSpeciesProfile("orange", 20, .85f, 1.26f, 3.29f,
                    new Color(.66f,.65f,.55f), new Color(.69f,.67f,.57f),
                    new Color(.26f,.49f,.24f), .21f,.32f,.16f,.22f,.93f,
                    new Color(.98f,.67f,.24f),.73f,.84f,.24f) },
                { "apricot", new FruitSpeciesProfile("apricot", 22, .96f, 1.25f, 3.40f,
                    new Color(.72f,.64f,.59f), new Color(.77f,.68f,.60f),
                    new Color(.32f,.56f,.22f), .18f,.27f,.13f,.19f,.52f,
                    new Color(.98f,.70f,.36f),.60f,.70f,.28f) },
                { "plum", new FruitSpeciesProfile("plum", 20, .91f, 1.30f, 3.60f,
                    new Color(.65f,.60f,.58f), new Color(.71f,.65f,.62f),
                    new Color(.28f,.53f,.24f), .19f,.29f,.13f,.20f,.62f,
                    new Color(.36f,.18f,.56f),.60f,.71f,.28f) },
                { "lemon", new FruitSpeciesProfile("lemon", 19, .87f, 1.24f, 3.28f,
                    new Color(.69f,.66f,.54f), new Color(.73f,.70f,.60f),
                    new Color(.30f,.51f,.25f), .21f,.32f,.14f,.20f,.90f,
                    new Color(.96f,.88f,.29f),.70f,.83f,.29f) },
                { "mandarin", new FruitSpeciesProfile("mandarin", 21, .87f, 1.18f, 3.12f,
                    new Color(.67f,.62f,.55f), new Color(.74f,.68f,.57f),
                    new Color(.28f,.51f,.24f), .18f,.29f,.15f,.22f,.92f,
                    new Color(.98f,.57f,.19f),.68f,.79f,.27f) },
                { "pomegranate", new FruitSpeciesProfile("pomegranate", 17, .88f,
                    1.35f, 3.18f, new Color(.73f,.62f,.54f),
                    new Color(.79f,.66f,.57f), new Color(.31f,.55f,.23f),
                    .19f,.29f,.13f,.19f,.68f,
                    new Color(.76f,.17f,.21f),.65f,.76f,.29f) }
            };

        public static bool IsSupported(string id) =>
            id != null && All.ContainsKey(id);

        public static FruitSpeciesProfile Get(string id)
        {
            if (id == null || !All.TryGetValue(id, out var profile))
                throw new ArgumentException("Unsupported tree species: " + id, nameof(id));
            return profile;
        }

        public static int Count => All.Count;

        public Mesh CreateFruitMesh()
        {
            switch (Id)
            {
                case "pear": return ProceduralMesh.Pear();
                case "cherry": return ProceduralMesh.Cherry();
                case "peach": return ProceduralMesh.Peach();
                case "orange": return ProceduralMesh.Orange();
                case "apricot": return ProceduralMesh.Apricot();
                case "plum": return ProceduralMesh.Plum();
                case "lemon": return ProceduralMesh.Lemon();
                case "mandarin": return ProceduralMesh.Mandarin();
                case "pomegranate": return ProceduralMesh.Pomegranate();
                default: return ProceduralMesh.Apple();
            }
        }
    }
}
