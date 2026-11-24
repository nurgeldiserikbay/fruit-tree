using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Seeded orchard tree: one merged wood mesh, one merged leaf mesh, separate pickable fruit.</summary>
    public sealed class TreeGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 122;
        [SerializeField, Range(8, 300)] private int appleCount = 30;
        [SerializeField] private string speciesId = "apple";
        [SerializeField] private bool generateAutomatically = true;

        /// <summary>
        /// The generated orchard scene configures the correct species at Start.
        /// Disabling the initial 30-fruit generation avoids a costly duplicate.
        /// Standalone procedural tree scenes keep the previous default behavior.
        /// </summary>
        public void SetAutomaticGeneration(bool enabled)
        {
            generateAutomatically = enabled;
        }

        public string SpeciesId => speciesId;
        public bool ReducedMotion { get; private set; }
        [SerializeField, Range(4, 25)] private int leavesPerCluster = 19;
        private readonly List<Apple> apples = new List<Apple>();
        public IReadOnlyList<Apple> Apples => apples;
        public event Action<int, int> HarvestChanged;
        public event Action<Apple> FruitPicked;
        public int HarvestedCount { get; private set; }
        public int TotalApples => appleCount;
        private System.Random random;
        private readonly List<UnityEngine.Object> runtimeResources = new List<UnityEngine.Object>();
        private Coroutine rebloomAnimation;
        private GameObject blossomObject;
        public bool IsReblooming { get; private set; }

        private T Track<T>(T resource) where T : UnityEngine.Object
        {
            runtimeResources.Add(resource);
            return resource;
        }

        private void ReleaseResources()
        {
            foreach (var resource in runtimeResources)
            {
                if (resource == null) continue;
                if (Application.isPlaying) Destroy(resource);
                else DestroyImmediate(resource);
            }
            runtimeResources.Clear();
        }

        private void OnDestroy()
        {
            CancelRebloom();
            ReleaseResources();
        }

        private void CancelRebloom()
        {
            if (rebloomAnimation != null)
            {
                StopCoroutine(rebloomAnimation);
                rebloomAnimation = null;
            }
            IsReblooming = false;
            if (blossomObject != null) blossomObject.SetActive(false);
        }

        private void Awake()
        {
            ReducedMotion = PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) == 1;
            // The scene intentionally keeps procedural mesh objects out of serialized scene data.
            // Build them reliably on launch so Android and Editor display the same content.
            if (Application.isPlaying && generateAutomatically && apples.Count == 0)
                Generate();
        }

        public void SetReducedMotion(bool reduce)
        {
            ReducedMotion = reduce;
            PlayerPrefs.SetInt("FruitTree.ReducedMotion", reduce ? 1 : 0);
            PlayerPrefs.Save();
            if (reduce && IsReblooming) ResetHarvest();
            Transform leafObject = transform.Find("Merged Leaves");
            if (leafObject == null) return;
            var renderer = leafObject.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null &&
                renderer.sharedMaterial.HasProperty("_Wind"))
                renderer.sharedMaterial.SetFloat("_Wind", reduce ? .008f : .045f);
        }

        public void Configure(string species, int fruitCount, int generationSeed)
        {
            if (!FruitSpeciesProfile.IsSupported(species))
                throw new ArgumentException("Unsupported tree species: " + species);
            if (fruitCount < 1 || fruitCount > 300)
                throw new ArgumentOutOfRangeException(nameof(fruitCount));
            speciesId = species;
            appleCount = fruitCount;
            seed = generationSeed;
            Generate();
        }

        public void RestorePickedFruits(IReadOnlyCollection<string> picked)
        {
            if (picked == null) return;
            var pickedSet = new HashSet<string>(picked, StringComparer.Ordinal);
            HarvestedCount = 0;
            foreach (var fruit in apples)
            {
                if (pickedSet.Contains(fruit.StableFruitId))
                {
                    fruit.RestorePicked();
                    HarvestedCount++;
                }
            }
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        public void Generate()
        {
            CancelRebloom();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false); // old fruit cannot be tapped during scene switch
                    Destroy(child);
                }
                else DestroyImmediate(child);
            }
            apples.Clear();
            ReleaseResources();
            blossomObject = null;
            HarvestedCount = 0;
            if (Application.isPlaying && SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000)
                leavesPerCluster = Mathf.Min(leavesPerCluster, 8);
            random = new System.Random(seed);
            FruitSpeciesProfile profile = FruitSpeciesProfile.Get(speciesId);
            // Photographic foliage already includes green pigment. Multiplying
            // it by the procedural green vertex tint makes it muddy/dark.
            // Resolve the optional artwork once before building any leaves.
            Texture2D leafArt = Resources.Load<Texture2D>(
                "TreeTextures/Leaves/" + speciesId);
            if (leafArt == null)
                leafArt = Resources.Load<Texture2D>("TreeTextures/Leaves/Default");
            // Every species shares the real photo leaf, tinted to its own green.
            if (leafArt == null)
                leafArt = Resources.Load<Texture2D>("TreeTextures/Leaves/apple");
            bool texturedLeaves = leafArt != null;
            var wood = new ProceduralMesh();
            var foliage = new ProceduralMesh();
            var fruitAnchors = new List<Vector3>();
            // Photographic bark already contains its colour; the darker
            // procedural tint would multiply it into a near-black trunk.
            bool photoBark = Resources.Load<Texture2D>("TreeTextures/Bark") != null;
            Color brown = photoBark ? new Color(.96f, .92f, .88f) : profile.TrunkColor;
            Color branchColor = photoBark ? new Color(.90f, .86f, .80f) : profile.BranchColor;
            // A slight bend in the trunk and irregularly-spaced limbs avoid the
            // artificial radial symmetry of the first prototype.
            const float goldenRootAngle = 2.39996323f;
            Vector3 trunkBase = Vector3.zero;
            Vector3 trunkLow = new Vector3(.055f, profile.TrunkHeight * .30f, -.025f);
            Vector3 trunkMid = new Vector3(-.04f, profile.TrunkHeight * .63f, .06f);
            Vector3 trunkTop = new Vector3(.04f, profile.TrunkHeight, -.025f);
            wood.BranchCurve(
                new[] { trunkBase, trunkLow, trunkMid, trunkTop },
                new[] { .33f, .27f, .19f, .035f }, brown);
            // Subtle roots merge the trunk into the soil without an oversized base.
            // One merged wood mesh; no physics objects or extra draw calls.
            for (int root = 0; root < 5; root++)
            {
                float a = root * goldenRootAngle + .21f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                // Photo bark: broad flares that sink into the soil instead of spikes.
                if (photoBark)
                    wood.Branch(trunkBase + Vector3.up * .20f + outward * .16f,
                        outward * (.47f + root * .025f) + Vector3.down * .13f,
                        .15f, .06f, brown);
                else
                    wood.Branch(trunkBase + Vector3.up * .31f + outward * .22f,
                        outward * (.42f + root * .025f) + Vector3.down * .047f,
                        .085f, .012f, brown);
            }

            const float goldenAngle = 2.39996323f;
            int majorBranches = profile.BranchCount;
            float crownSpread = profile.CrownSpread;
            // Photoreal pass for every species: finer curved limbs, secondary
            // twigs, a shaded crown volume and fruit near the outside.
            bool photoTree = texturedLeaves;
            bool detailedTree = photoTree || speciesId == "apple";
            Color photoLeafColor = photoTree ? profile.PhotoLeafTint : Color.white;
            // A tall dome instead of flat horizontal tiers; its shape varies by species.
            float crownTop = photoTree ? profile.CrownHeightHigh + profile.PhotoCrownLift :
                profile.FillerTopHeight;
            Vector3 crownCenter = Vector3.up * ((profile.CrownHeightLow + crownTop) * .5f);
            float crownRadius = (photoTree ? 2.8f : 2.65f) * crownSpread;
            int twigCount = detailedTree ? 7 : profile.BranchCount >= 21 ? 6 : 5;
            int appleLeafCount = detailedTree ? Mathf.Min(leavesPerCluster + 1, 22) :
                Mathf.Min(leavesPerCluster + 1, 21);
            // More limbs with fewer leaves each: a fuller tree at the same mesh budget.
            if (photoTree)
            {
                majorBranches = profile.PhotoLimbs;
                appleLeafCount = Mathf.Min(appleLeafCount, 14);
            }
            for (int i = 0; i < majorBranches; i++)
            {
                float elevation = i / (float)(majorBranches - 1);
                // A real crown has overlapping high and low lobes rather
                // than one flat disc. Introduce broad vertical variation,
                // without changing branch/fruit counts or stable IDs.
                float crownWave = Mathf.Sin(goldenAngle * i * 2f + .4f) * .28f;
                float height = Mathf.Lerp(profile.CrownHeightLow + (photoTree ? .55f : 0f),
                    profile.CrownHeightHigh, elevation) +
                    crownWave + Between(-.19f, .19f);
                float angle = goldenAngle * i + Between(-.18f, .18f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 sideways = new Vector3(-radial.z, 0f, radial.x);
                // Break the perfectly circular umbrella outline into a few
                // broad lobes, while preserving stable fruit IDs and counts.
                float canopyLobes = 1f + .12f * Mathf.Sin(angle * 3f + .8f) +
                    .055f * Mathf.Sin(angle * 5f - .6f);
                float length = (photoTree ? Mathf.Lerp(2.7f, 1.25f, Mathf.Pow(elevation, 1.4f)) :
                    2.65f - elevation * 1.2f) * crownSpread *
                    canopyLobes * Between(.84f, 1.10f);
                // Low limbs reach outward, high limbs climb steeply: a rounded dome.
                float limbRise = photoTree ? Mathf.Lerp(.15f, profile.PhotoLimbRise, elevation * elevation) :
                    profile.BranchRise;
                Vector3 origin = new Vector3(Between(-.065f,.065f), height, Between(-.065f,.065f));

                int limbSections = photoTree ? 7 : 3;
                var limbPoints = new Vector3[limbSections + 1];
                var limbRadii = new float[limbSections + 1];
                limbPoints[0] = origin;
                float baseRadius = Mathf.Lerp(.14f, .095f, elevation) * (photoTree ? .62f : 1f);
                limbRadii[0] = baseRadius;
                float bend = photoTree ? Between(-.55f, .55f) : Between(-.16f, .16f);
                float wigglePhase = photoTree ? Between(0f, 6.28f) : 0f;
                Vector3 LimbCurve(float along) => !photoTree ? Vector3.zero :
                    sideways * (Mathf.Sin(along * Mathf.PI) * bend +
                        Mathf.Sin(along * 9f + wigglePhase) * .055f * along) +
                    // Limbs leave the trunk upward and arch out, not as straight logs.
                    Vector3.up * (Mathf.Sin(along * 7f + wigglePhase) * .05f * along +
                        .62f * along * (1f - along));
                for (int section = 1; section <= limbSections; section++)
                {
                    float fraction = section / (float)limbSections;
                    limbPoints[section] = origin + radial * (length * fraction) +
                        (photoTree ? LimbCurve(fraction) :
                            sideways * (Mathf.Sin(fraction * Mathf.PI) * bend)) +
                        Vector3.up * (.12f * fraction +
                            limbRise * fraction * fraction);
                    limbRadii[section] = Mathf.Lerp(baseRadius, photoTree ? .016f : .022f, fraction);
                }
                wood.BranchCurve(limbPoints, limbRadii, branchColor);

                // Each limb carries several smaller twigs with two leaf clusters.
                // All wood/leaves are merged into just two renderer objects.
                for (int twigIndex = 0; twigIndex < twigCount; twigIndex++)
                {
                    float fraction = detailedTree ? .24f + twigIndex * .105f :
                        .32f + twigIndex * .14f;
                    float sideSign = twigIndex % 2 == 0 ? 1f : -1f;
                    Vector3 start = origin + radial * (length * fraction) + LimbCurve(fraction) +
                        Vector3.up * (.12f * fraction +
                            limbRise * fraction * fraction);
                    Vector3 twigDirection = (radial * .65f +
                        sideways * sideSign * Between(.28f, .7f) +
                        Vector3.up * (Between(.18f,.58f) *
                            profile.TwigUpwardFactor)).normalized;
                    float twigLength = Between(.44f, detailedTree ? .94f : .82f) *
                        (1f - elevation * .18f);
                    Vector3 end = start + twigDirection * twigLength;
                    wood.Branch(start, end, photoTree ? .024f : .039f, .006f, branchColor);
                    if (detailedTree)
                    {
                        // Forked fine shoots enrich silhouette without a new renderer.
                        Vector3 shootStart = Vector3.Lerp(start, end, .62f);
                        Vector3 shootTip = shootStart + (twigDirection +
                            sideways * sideSign * .65f + Vector3.up * .28f).normalized * .32f;
                        wood.Branch(shootStart, shootTip, .012f, .003f, branchColor);
                    }

                    for (int clusterIndex = 0; clusterIndex < 3; clusterIndex++)
                    {
                        float along = .36f + clusterIndex * .29f;
                        // Alternate cluster height to avoid flat rows of
                        // leaf cards on every secondary shoot.
                        Vector3 center = Vector3.Lerp(start, end, along) +
                            Vector3.up * Between(-.13f, .21f);
                        fruitAnchors.Add(center);
                        for (int leafIndex = 0; leafIndex < appleLeafCount; leafIndex++)
                        {
                            // Leaves grow in sprays around their twig,
                            // not as an isotropic cloud of random polygons.
                            // Bias each leaf outwards from the branch with
                            // alternating lateral spread and slight droop.
                            float leafAngle = leafIndex * goldenAngle +
                                Between(-.22f, .22f);
                            float sprayRadius = Between(.13f, .39f);
                            Vector3 spread = sideways * (Mathf.Cos(leafAngle) * sprayRadius) +
                                Vector3.up * (Mathf.Sin(leafAngle) * sprayRadius * .72f);
                            Vector3 offset = twigDirection * Between(.07f, .23f) +
                                spread + Vector3.up * Between(-.08f, .09f);
                            Vector3 direction = (twigDirection * .52f +
                                spread.normalized * .74f +
                                Vector3.up * Between(-.08f, .24f)).normalized;
                            // Photo leaves need a wider tone range and a smaller
                            // size, or the crown reads as flat overlapping cards.
                            float tint = Between(-.065f,.065f) * (texturedLeaves ? 2.6f : 1f);
                            float photoScale = photoTree ? Between(.62f, .95f) :
                                texturedLeaves ? .72f : 1f;
                            Color leafColor = (texturedLeaves
                                ? photoLeafColor : profile.LeafColor) +
                                new Color(tint, tint * .70f, tint * .48f);
                            Vector3 leafPoint = center + offset * .75f;
                            if (photoTree)
                                // Each cluster reads as a puff: lit on top, darker beneath.
                                leafColor *= CrownShade(leafPoint, crownCenter, crownRadius) *
                                    Mathf.Lerp(.86f, 1.1f, Mathf.InverseLerp(-.3f, .3f, offset.y));
                            // Slightly rounder, shorter foliage cards read as
                            // a soft canopy instead of isolated spear tips.
                            foliage.Leaf(leafPoint, direction,
                                Between(profile.LeafLengthLow, profile.LeafLengthHigh) *
                                    (detailedTree ? .72f : .82f) * photoScale,
                                Between(profile.LeafWidthLow, profile.LeafWidthHigh) *
                                    (detailedTree ? .78f : .91f) * photoScale,
                                leafColor, profile.LeafOvalness,
                                photoTree ? ((leafPoint - crownCenter).normalized +
                                    offset.normalized * .7f).normalized : (Vector3?)null);
                        }
                    }
                }
            }
            // Lightweight crown fillers close large artificial gaps above the
            // highest limbs while keeping most apples readable from outside.
            int fillerCount = detailedTree ? 31 : 27;
            for (int cluster = 0; cluster < fillerCount; cluster++)
            {
                float heightRatio = (cluster + .5f) / fillerCount;
                float angle = cluster * goldenAngle;
                float radius = Mathf.Lerp(1.6f, .55f, heightRatio) *
                    profile.FillerSpread * Between(.75f, 1.1f);
                Vector3 center = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Lerp(profile.CrownHeightLow + 1.2f,
                        crownTop, heightRatio),
                    Mathf.Sin(angle) * radius);
                for (int leafIndex = 0; leafIndex < 9; leafIndex++)
                {
                    Vector3 offset = new Vector3(
                        Between(-.32f,.32f), Between(-.24f,.23f), Between(-.32f,.32f));
                    Vector3 direction = (offset + new Vector3(
                        Between(-.4f,.4f), .13f, Between(-.4f,.4f))).normalized;
                    float shade = Between(-.055f,.07f);
                    Vector3 fillerPoint = center + offset;
                    float fillerScale = photoTree ? Between(.55f, .85f) : 1f;
                    Color fillerColor = (texturedLeaves ? photoLeafColor : profile.LeafColor) +
                        new Color(shade, shade, shade * .5f);
                    if (photoTree)
                        fillerColor *= CrownShade(fillerPoint, crownCenter, crownRadius);
                    foliage.Leaf(fillerPoint, direction,
                        Between(detailedTree ? .15f : .20f,
                            detailedTree ? .23f : .30f) * fillerScale,
                        Between(detailedTree ? .11f : .15f,
                            detailedTree ? .16f : .21f) * fillerScale,
                        fillerColor,
                        profile.LeafOvalness,
                        photoTree ? (fillerPoint - crownCenter).normalized : (Vector3?)null);
                }
            }
            // Photo crown: hang fruit from the outer clusters so it stays visible.
            if (photoTree)
            {
                fruitAnchors.Sort((left, right) =>
                    (right - crownCenter).sqrMagnitude.CompareTo(
                        (left - crownCenter).sqrMagnitude));
                int keep = Mathf.Max(appleCount, Mathf.CeilToInt(fruitAnchors.Count * .55f));
                if (keep < fruitAnchors.Count)
                    fruitAnchors.RemoveRange(keep, fruitAnchors.Count - keep);
            }
            // Prevent multiple fruits overlapping a single leaf cluster.
            for (int i = fruitAnchors.Count - 1; i > 0; i--)
            {
                int index = random.Next(i + 1);
                Vector3 swap = fruitAnchors[i];
                fruitAnchors[i] = fruitAnchors[index];
                fruitAnchors[index] = swap;
            }
            var barkMaterial = Track(VertexMaterial("Wood", Color.white));
            if (barkMaterial.HasProperty("_MainTex"))
            {
                Texture2D barkArt = Resources.Load<Texture2D>("TreeTextures/Bark");
                barkMaterial.SetTexture("_MainTex", barkArt != null ? barkArt :
                    Track(BarkTexture.Create()));
            }
            MakeMeshObject("Merged Wood", Track(wood.Build("Wood")), barkMaterial);
            var leafMaterial = Track(VertexMaterial("Leaves", Color.white));
            if (leafMaterial.HasProperty("_Wind"))
                leafMaterial.SetFloat("_Wind", ReducedMotion ? .008f : .045f);
            if (leafMaterial.HasProperty("_AmbientLight")) leafMaterial.SetFloat("_AmbientLight", .72f);
            if (leafMaterial.HasProperty("_LeafTranslucency"))
                leafMaterial.SetFloat("_LeafTranslucency", .28f);
            if (leafMaterial.HasProperty("_Cull")) leafMaterial.SetFloat("_Cull", 0f);
            // Alpha artwork is optional. The original opaque geometry remains
            // the default, while picking still uses the canopy MeshCollider.
            if (leafArt != null)
            {
                if (leafMaterial.HasProperty("_MainTex"))
                    leafMaterial.SetTexture("_MainTex", leafArt);
                if (leafMaterial.HasProperty("_AlphaCutoff"))
                    leafMaterial.SetFloat("_AlphaCutoff", .45f);
            }
            MakeMeshObject("Merged Leaves", Track(foliage.Build("Leaves")), leafMaterial);
            Mesh appleMesh = Track(profile.CreateFruitMesh());
            Color fruitColor = photoTree ? profile.FruitColor * profile.PhotoFruitTint :
                profile.FruitColor;
            // Different peels reflect light differently. Apples and cherries
            // have a waxy skin, peaches/apricots are softer and matte, while
            // citrus and pomegranate sit between those extremes.
            float peelGloss = speciesId == "apple" ? .53f :
                speciesId == "cherry" ? .63f :
                speciesId == "plum" ? .49f :
                speciesId == "pear" ? .42f :
                speciesId == "peach" || speciesId == "apricot" ? .24f :
                speciesId == "orange" || speciesId == "mandarin" ||
                speciesId == "lemon" ? .34f : .41f;
            Material appleMaterial = Track(LitMaterial("Fruit " + speciesId,
                fruitColor, peelGloss));
            if (appleMaterial.HasProperty("_MainTex"))
            {
                // Imported artwork takes priority, with the existing lightweight
                // generated texture as fallback. Resource assets belong to Unity
                // and must never be destroyed as transient generated objects.
                Texture2D fruitArt = Resources.Load<Texture2D>(
                    "TreeTextures/Fruit/" + speciesId);
                appleMaterial.SetTexture("_MainTex", fruitArt != null ? fruitArt :
                    Track(AppleSkinTexture.CreateForSpecies(speciesId)));
                // External fruit art already contains the species hue. Applying
                // FruitColor again multiplies that color and makes imported
                // apples, lemons and plums unnaturally dark or oversaturated.
                // The generated fallback keeps its established colored tint.
                if (fruitArt != null && appleMaterial.HasProperty("_Color"))
                    appleMaterial.SetColor("_Color", Color.white);
            }
            // Just three shared skin variants per species: natural color variation
            // without allocating an individual material for every fruit.
            var skins = new Material[3];
            skins[0] = appleMaterial;
            for (int variant = 1; variant < skins.Length; variant++)
            {
                var tone = new Material(appleMaterial) { name = "Fruit Skin Variant " + variant };
                Color tint = variant == 1 ?
                    new Color(1f, .93f, .89f) : new Color(.93f, 1f, .93f);
                Color baseTint = appleMaterial.HasProperty("_Color")
                    ? appleMaterial.GetColor("_Color") : fruitColor;
                tone.color = new Color(baseTint.r * tint.r,
                    baseTint.g * tint.g, baseTint.b * tint.b, baseTint.a);
                skins[variant] = Track(tone);
            }
            Material stemMaterial = Track(LitMaterial("Stem", new Color(.28f,.19f,.095f), .12f));
            for (int i = 0; i < appleCount; i++)
            {
                Vector3 attachment = fruitAnchors[i * fruitAnchors.Count / appleCount];
                Vector3 outward = new Vector3(attachment.x, 0f, attachment.z).normalized;
                if (photoTree) attachment += outward * .16f;
                // Smaller fruit tuck underneath twig ends and leaves rather than
                // sitting like large red spheres on the outside of the crown.
                Vector3 at = attachment + Vector3.down * Between(.19f, .26f) *
                    (photoTree ? .6f : 1f) +
                    outward * (i % 4 == 0 ? .025f : .105f) +
                    new Vector3(Between(-.05f, .05f), 0f, Between(-.05f, .05f));
                var fruit = MakeMeshObject(speciesId + " " + (i + 1), appleMesh,
                    skins[i % skins.Length]);
                fruit.transform.localPosition = at;
                // Gentle size variety with ~30% smaller apples than the early prototype.
                float photoFruitSize = photoTree ? profile.PhotoFruitSize * .85f : 1f;
                float fruitScale = Between(profile.FruitScaleLow, profile.FruitScaleHigh) *
                    photoFruitSize;
                fruit.transform.localScale = Vector3.one * fruitScale;
                fruit.transform.localRotation = Quaternion.Euler(Between(-10, 10), Between(0, 360), Between(-10, 10));
                // Smaller art keeps the same finger-sized tap target.
                fruit.AddComponent<SphereCollider>().radius = profile.HitRadius / photoFruitSize;
                var component = fruit.AddComponent<Apple>();
                component.Initialize(i);
                component.Harvested += OnHarvest;
                apples.Add(component);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(fruit.transform, false);
                // Attach the end of the stem to the exact twig anchor, rather than
                // letting apples appear to float when viewed from behind.
                Vector3 top = fruit.transform.TransformPoint(new Vector3(0f,
                    appleMesh.bounds.max.y, 0f));
                Vector3 twig = transform.TransformPoint(attachment);
                Vector3 axis = twig - top;
                stem.transform.position = (top + twig) * .5f;
                stem.transform.rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
                // The stem is a child of a scaled fruit. Compensate for that
                // parent scale so its world-space ends actually meet the twig.
                float inverseFruitScale = 1f / fruitScale;
                stem.transform.localScale = new Vector3(.012f * inverseFruitScale,
                    axis.magnitude * .5f * inverseFruitScale,
                    .012f * inverseFruitScale);
                stem.GetComponent<Renderer>().sharedMaterial = stemMaterial;
                if (Application.isPlaying) Destroy(stem.GetComponent<Collider>());
                else DestroyImmediate(stem.GetComponent<Collider>());
            }
            HarvestChanged?.Invoke(0, appleCount);
        }

        /// <summary>
        /// Start a new picking round without recreating meshes or materials.
        /// This avoids garbage collection spikes on Android and keeps the scene stable.
        /// </summary>
        public void ResetHarvest()
        {
            CancelRebloom();
            if (apples.Count == 0)
            {
                Generate();
                return;
            }
            foreach (var apple in apples)
                if (apple != null) apple.ResetApple();
            HarvestedCount = 0;
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        /// <summary>
        /// Lightweight staggered regrowth using one coroutine for all fruits.
        /// The saved domain state is reset before this cosmetic animation starts.
        /// </summary>
        public void ResetHarvestAnimated()
        {
            // Respect the accessibility setting: all newly available fruit
            // becomes tappable immediately, without scale or blossom motion.
            if (!Application.isPlaying || ReducedMotion)
            {
                ResetHarvest();
                return;
            }

            CancelRebloom();
            if (apples.Count == 0)
            {
                Generate();
                return;
            }
            for (int i = 0; i < apples.Count; i++)
                if (apples[i] != null) apples[i].BeginRebloom();
            if (blossomObject == null) CreateBlossoms();
            if (blossomObject != null) blossomObject.SetActive(true);

            HarvestedCount = 0;
            HarvestChanged?.Invoke(0, appleCount);
            IsReblooming = true;
            rebloomAnimation = StartCoroutine(AnimateRebloom());
        }

        private System.Collections.IEnumerator AnimateRebloom()
        {
            const float duration = 1.95f;
            const float flowerTime = .88f;
            const float growthDelay = .30f;
            const float stagger = .65f;
            const float growDuration = 1.0f;
            float time = 0f;
            while (time < duration)
            {
                time = Mathf.Min(duration, time + Time.unscaledDeltaTime);
                if (blossomObject != null && blossomObject.activeSelf != (time < flowerTime))
                    blossomObject.SetActive(time < flowerTime);
                for (int i = 0; i < apples.Count; i++)
                {
                    Apple fruit = apples[i];
                    if (fruit == null) continue;
                    float offset = ((i * 37) % apples.Count) /
                        (float)Mathf.Max(1, apples.Count - 1) * stagger;
                    fruit.GrowRebloom((time - growthDelay - offset) / growDuration);
                }
                yield return null;
            }
            for (int i = 0; i < apples.Count; i++)
                if (apples[i] != null) apples[i].GrowRebloom(1f);
            if (blossomObject != null) blossomObject.SetActive(false);
            rebloomAnimation = null;
            IsReblooming = false;
        }

        private void CreateBlossoms()
        {
            // A single merged blossom mesh instead of hundreds of particle
            // emitters/GameObjects. Kept dormant and reused for further rounds.
            var flowers = new ProceduralMesh();
            Color petal = speciesId == "cherry" || speciesId == "plum" ?
                new Color(.97f, .80f, .85f) :
                speciesId == "apricot" ? new Color(1f, .89f, .79f) :
                speciesId == "pomegranate" ? new Color(.96f, .72f, .70f) :
                speciesId == "pear" ? new Color(.97f, .98f, .92f) :
                new Color(.98f, .90f, .86f);
            for (int i = 0; i < apples.Count; i++)
            {
                Apple fruit = apples[i];
                if (fruit == null) continue;
                Vector3 center = fruit.transform.localPosition + Vector3.up * .03f;
                for (int j = 0; j < 5; j++)
                {
                    float angle = (j * 2f * Mathf.PI / 5f) + i * .11f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), .16f,
                        Mathf.Sin(angle)).normalized;
                    flowers.Leaf(center, direction, .095f, .065f,
                        petal * (j % 2 == 0 ? 1f : .94f));
                }
            }
            var material = Track(VertexMaterial("Rebloom Petals", Color.white));
            if (material.HasProperty("_Wind")) material.SetFloat("_Wind", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            blossomObject = MakeMeshObject("Merged Rebloom Flowers",
                Track(flowers.Build("Rebloom Flower Petals")), material);
            blossomObject.SetActive(false);
        }

        private void OnHarvest(Apple apple)
        {
            HarvestedCount++;
            FruitPicked?.Invoke(apple);
            HarvestChanged?.Invoke(HarvestedCount, appleCount);
        }

        private GameObject MakeMeshObject(string label, Mesh mesh, Material material)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (label == "Merged Wood" || label == "Merged Leaves")
            {
                // A single non-convex collider keeps leaves visually opaque
                // to picking rays. Without it, hidden fruit is collected through
                // the canopy. Never add individual leaf GameObjects/colliders.
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            // Avoid dozens of dynamic shadow-casting meshes on constrained phones.
            if (label == "Merged Leaves" || label == "Merged Rebloom Flowers" ||
                label.StartsWith("apple ") ||
                label.StartsWith("pear ") || label.StartsWith("cherry ") ||
                label.StartsWith("peach ") || label.StartsWith("orange ") ||
                label.StartsWith("apricot ") || label.StartsWith("plum ") ||
                label.StartsWith("lemon ") || label.StartsWith("mandarin ") ||
                label.StartsWith("pomegranate "))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Material VertexMaterial(string label, Color fallback)
        {
            Shader shader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = label };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallback);
            return mat;
        }

        private static Material LitMaterial(string label, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = label, color = color };
            // Hundreds of fruit share only three materials and a single mesh.
            // Standard's instancing path can reduce draw submissions on GLES3.
            if (shader != null && shader.name == "Standard" && SystemInfo.supportsInstancing)
                mat.enableInstancing = true;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        // Baked canopy shading: leaves deep inside or under the crown get less sky.
        private static float CrownShade(Vector3 point, Vector3 center, float radius)
        {
            Vector3 fromCore = point - center;
            float depth = Mathf.InverseLerp(.25f, .95f, fromCore.magnitude / radius);
            float outer = Mathf.Lerp(.62f, 1.18f, Mathf.SmoothStep(0f, 1f, depth));
            return outer * Mathf.Lerp(.8f, 1.05f, Mathf.InverseLerp(-1.6f, 1.2f, fromCore.y));
        }

        private float Between(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
