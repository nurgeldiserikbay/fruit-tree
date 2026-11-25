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
        public float BranchRise { get; private set; }
        public float TwigUpwardFactor { get; private set; }
        public float TrunkHeight { get; private set; }
        public float FillerTopHeight { get; private set; }
        public float FillerSpread { get; private set; }

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
            // Shape the outline without increasing any mesh or leaf counts.
            // Upright pear, broader cherry/peach and more compact citrus.
            switch (id)
            {
                case "pear":
                    BranchRise = .72f; TwigUpwardFactor = 1.15f;
                    TrunkHeight = 4.22f; FillerTopHeight = 4.38f;
                    FillerSpread = .80f;
                    break;
                case "cherry":
                    BranchRise = .19f; TwigUpwardFactor = .75f;
                    TrunkHeight = 3.78f; FillerTopHeight = 3.92f;
                    FillerSpread = 1.12f;
                    break;
                case "peach":
                    BranchRise = .22f; TwigUpwardFactor = .79f;
                    TrunkHeight = 3.84f; FillerTopHeight = 4.05f;
                    FillerSpread = 1.1f;
                    break;
                case "orange":
                case "mandarin":
                    BranchRise = .34f; TwigUpwardFactor = .95f;
                    TrunkHeight = 3.8f; FillerTopHeight = 3.97f;
                    FillerSpread = .9f;
                    break;
                case "pomegranate":
                    BranchRise = .27f; TwigUpwardFactor = .9f;
                    TrunkHeight = 3.85f; FillerTopHeight = 4f;
                    FillerSpread = .92f;
                    break;
                case "lemon":
                    BranchRise = .46f; TwigUpwardFactor = 1.05f;
                    TrunkHeight = 3.94f; FillerTopHeight = 4.12f;
                    FillerSpread = .91f;
                    break;
                case "plum":
                    BranchRise = .55f; TwigUpwardFactor = 1.05f;
                    TrunkHeight = 4.10f; FillerTopHeight = 4.3f;
                    FillerSpread = .92f;
                    break;
                case "apricot":
                    BranchRise = .33f; TwigUpwardFactor = .92f;
                    TrunkHeight = 3.94f; FillerTopHeight = 4.12f;
                    FillerSpread = 1.03f;
                    break;
                default:
                    BranchRise = .48f; TwigUpwardFactor = 1f;
                    TrunkHeight = 4.02f; FillerTopHeight = 4.25f;
                    FillerSpread = 1f;
                    break;
            }
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

        // Growth habit of the photoreal crown. Each species gets a recognisable
        // silhouette: narrow upright pear, wide layered cherry, open vase-shaped
        // peach, low dense citrus balls and a many-stemmed pomegranate bush.
        private bool IsCitrus => Id == "orange" || Id == "lemon" || Id == "mandarin";

        /// <summary>Limbs in the photo-textured crown.</summary>
        public int PhotoLimbs => Id == "peach" ? 26 : Id == "pomegranate" ? 28 :
            Id == "cherry" ? 33 : Id == "pear" || Id == "plum" || IsCitrus ? 30 : 32;

        /// <summary>How far the crown dome rises above the highest limb origin.</summary>
        public float PhotoCrownLift => Id == "pear" ? 3f : Id == "plum" ? 2.6f :
            Id == "cherry" ? 1.5f : Id == "peach" ? 1.7f : Id == "apricot" ? 2f :
            Id == "mandarin" ? 1.8f : IsCitrus || Id == "pomegranate" ? 1.95f : 2.3f;

        /// <summary>Climb of the highest limbs: high for upright trees, low for spreading ones.</summary>
        public float PhotoLimbRise => Id == "pear" ? 3.4f : Id == "plum" ? 3f :
            Id == "cherry" ? 1.4f : Id == "peach" || Id == "apricot" || Id == "mandarin" ? 2f :
            Id == "orange" ? 2.1f : Id == "lemon" ? 2.3f : Id == "pomegranate" ? 2.4f : 2.5f;

        /// <summary>Climb of the lowest limbs: a vase or upright tree lifts even those.</summary>
        public float PhotoLowRise => Id == "peach" ? 1f : Id == "pear" ? .9f :
            Id == "pomegranate" ? .6f : Id == "plum" ? .5f : Id == "cherry" ? .05f : .15f;

        /// <summary>Limb length relative to the apple: crown width.</summary>
        public float PhotoReach => Id == "cherry" ? 1.22f : Id == "peach" ? 1.15f :
            Id == "apricot" ? 1.08f : Id == "pomegranate" ? .9f : Id == "lemon" ? .88f :
            Id == "plum" ? .86f : Id == "orange" ? .84f : Id == "mandarin" ? .86f :
            Id == "pear" ? .74f : 1f;

        /// <summary>Height of the lowest limbs above the profile's crown base.</summary>
        public float PhotoCrownBase => Id == "pear" ? .85f : Id == "plum" ? .75f :
            Id == "cherry" ? .45f : Id == "apricot" ? .35f : Id == "peach" ? .05f :
            Id == "pomegranate" ? -.05f : Id == "lemon" ? -.1f : Id == "orange" ? -.25f :
            Id == "mandarin" ? -.1f : .55f;

        /// <summary>Leaf size: long peach leaves, small pomegranate and mandarin leaves.</summary>
        public float PhotoLeafScale => Id == "peach" ? 1.15f : Id == "apricot" ? 1.05f :
            Id == "pomegranate" ? .72f : Id == "mandarin" ? .85f :
            Id == "cherry" || Id == "plum" ? .9f : IsCitrus || Id == "pear" ? .95f : 1f;

        /// <summary>Leaves per cluster: evergreen citrus and pomegranate are denser.</summary>
        public int PhotoLeafCount => Id == "mandarin" ? 13 :
            IsCitrus || Id == "peach" || Id == "pomegranate" ? 15 : 14;

        /// <summary>Bark colour: red-brown cherry, grey smooth citrus.</summary>
        public Color PhotoBarkTint => Id == "cherry" ? new Color(1f, .8f, .76f) :
            Id == "peach" ? new Color(.95f, .86f, .82f) : IsCitrus ? new Color(.86f, .86f, .8f) :
            Id == "pomegranate" ? new Color(.82f, .8f, .76f) : Color.white;

        /// <summary>Separate stems from the ground: the pomegranate grows as a bush.</summary>
        public int PhotoStems => Id == "pomegranate" ? 3 : 1;

        /// <summary>A short line about the tree for the tree list.</summary>
        public string Description
        {
            get
            {
                switch (Id)
                {
                    case "pear": return "A tall, narrow tree reaching for the sky, hung with soft green-gold pears.";
                    case "cherry": return "Wide, spreading branches heavy with small, shiny cherries.";
                    case "peach": return "An open, vase-shaped crown with long leaves and velvety peaches.";
                    case "orange": return "A dense evergreen ball of glossy leaves, bright with oranges.";
                    case "apricot": return "Broad, rounded leaves and sunny golden apricots.";
                    case "plum": return "An upright oval crown with deep purple plums.";
                    case "lemon": return "Dark glossy leaves and fragrant yellow lemons.";
                    case "mandarin": return "A low, compact citrus full of little mandarins.";
                    case "pomegranate": return "A many-stemmed bush with slender leaves and ruby pomegranates.";
                    default: return "Crisp red apples on a round, friendly crown. A gentle first tree.";
                }
            }
        }

        /// <summary>Fruit colour for menus and lists.</summary>
        public Color DisplayColor => Id == "apple" ? new Color(.80f, .16f, .13f) :
            FruitColor * PhotoFruitTint;

        /// <summary>Fruit size relative to the old prototype, in scale with the larger crown.</summary>
        public float PhotoFruitSize => Id == "cherry" ? .62f : Id == "apricot" ? .52f :
            Id == "pear" ? .42f :
            Id == "orange" || Id == "pomegranate" ? .46f :
            Id == "lemon" || Id == "mandarin" ? .44f : .5f;

        /// <summary>Tints the shared photo leaf toward this species' foliage colour.</summary>
        public Color PhotoLeafTint
        {
            get
            {
                Color reference = All["apple"].LeafColor;
                return new Color(
                    Mathf.Clamp(LeafColor.r / reference.r, .75f, 1.25f),
                    Mathf.Clamp(LeafColor.g / reference.g, .75f, 1.25f),
                    // Never above 1: extra blue turns the warm photo leaf teal.
                    Mathf.Clamp(LeafColor.b / reference.b, .75f, 1f));
            }
        }

        /// <summary>Calms peel colours that look neon under the forest light.</summary>
        public Color PhotoFruitTint => Id == "pear" ? new Color(.72f, .84f, 1.5f) :
            Id == "plum" ? new Color(.72f, .62f, .72f) :
            Id == "lemon" ? new Color(.93f, .90f, .75f) : Color.white;

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
