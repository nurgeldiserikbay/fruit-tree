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
            var wood = new ProceduralMesh();
            var foliage = new ProceduralMesh();
            var fruitAnchors = new List<Vector3>();
            Color brown = profile.TrunkColor;
            Color branchColor = profile.BranchColor;
            // A slight bend in the trunk and irregularly-spaced limbs avoid the
            // artificial radial symmetry of the first prototype.
            Vector3 trunkBase = Vector3.zero;
            Vector3 trunkLow = new Vector3(.055f, profile.TrunkHeight * .30f, -.025f);
            Vector3 trunkMid = new Vector3(-.04f, profile.TrunkHeight * .63f, .06f);
            Vector3 trunkTop = new Vector3(.04f, profile.TrunkHeight, -.025f);
            wood.BranchCurve(
                new[] { trunkBase, trunkLow, trunkMid, trunkTop },
                new[] { .43f, .33f, .22f, .035f }, brown);

            const float goldenAngle = 2.39996323f;
            int majorBranches = profile.BranchCount;
            float crownSpread = profile.CrownSpread;
            // Apple visual reference: finer leaves, more secondary twigs, and
            // a layered canopy. Other species retain their established geometry.
            bool detailedApple = speciesId == "apple";
            int twigCount = detailedApple ? 7 : 5;
            int appleLeafCount = detailedApple ? Mathf.Min(leavesPerCluster + 1, 22) : leavesPerCluster;
            for (int i = 0; i < majorBranches; i++)
            {
                float elevation = i / (float)(majorBranches - 1);
                float height = Mathf.Lerp(profile.CrownHeightLow,
                    profile.CrownHeightHigh, elevation) + Between(-.13f, .13f);
                float angle = goldenAngle * i + Between(-.18f, .18f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 sideways = new Vector3(-radial.z, 0f, radial.x);
                float length = (2.65f - elevation * 1.2f) * crownSpread * Between(.88f, 1.12f);
                Vector3 origin = new Vector3(Between(-.065f,.065f), height, Between(-.065f,.065f));

                var limbPoints = new Vector3[4];
                var limbRadii = new float[4];
                limbPoints[0] = origin;
                float baseRadius = Mathf.Lerp(.14f, .095f, elevation);
                limbRadii[0] = baseRadius;
                float bend = Between(-.16f, .16f);
                for (int section = 1; section <= 3; section++)
                {
                    float fraction = section / 3f;
                    limbPoints[section] = origin + radial * (length * fraction) +
                        sideways * (Mathf.Sin(fraction * Mathf.PI) * bend) +
                        Vector3.up * (.12f * fraction +
                            profile.BranchRise * fraction * fraction);
                    limbRadii[section] = Mathf.Lerp(baseRadius, .022f, fraction);
                }
                wood.BranchCurve(limbPoints, limbRadii, branchColor);

                // Each limb carries several smaller twigs with two leaf clusters.
                // All wood/leaves are merged into just two renderer objects.
                for (int twigIndex = 0; twigIndex < twigCount; twigIndex++)
                {
                    float fraction = detailedApple ? .24f + twigIndex * .105f :
                        .32f + twigIndex * .14f;
                    float sideSign = twigIndex % 2 == 0 ? 1f : -1f;
                    Vector3 start = origin + radial * (length * fraction) +
                        Vector3.up * (.12f * fraction +
                            profile.BranchRise * fraction * fraction);
                    Vector3 twigDirection = (radial * .65f +
                        sideways * sideSign * Between(.28f, .7f) +
                        Vector3.up * (Between(.18f,.58f) *
                            profile.TwigUpwardFactor)).normalized;
                    float twigLength = Between(.44f, detailedApple ? .94f : .77f) *
                        (1f - elevation * .18f);
                    Vector3 end = start + twigDirection * twigLength;
                    wood.Branch(start, end, .039f, .008f, branchColor);
                    if (detailedApple)
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
                        Vector3 center = Vector3.Lerp(start, end, along);
                        fruitAnchors.Add(center);
                        for (int leafIndex = 0; leafIndex < appleLeafCount; leafIndex++)
                        {
                            Vector3 offset = new Vector3(Between(-.39f,.39f),
                                Between(-.31f,.36f), Between(-.39f,.39f));
                            Vector3 direction = (offset +
                                new Vector3(Between(-.3f,.3f), .21f, Between(-.3f,.3f))).normalized;
                            float tint = Between(-.065f,.065f);
                            Color leafColor = profile.LeafColor +
                                new Color(tint, tint * .70f, tint * .48f);
                            // Slightly rounder, shorter foliage cards read as
                            // a soft canopy instead of isolated spear tips.
                            foliage.Leaf(center + offset * .75f, direction,
                                Between(profile.LeafLengthLow, profile.LeafLengthHigh) *
                                    (detailedApple ? .72f : .92f),
                                Between(profile.LeafWidthLow, profile.LeafWidthHigh) *
                                    (detailedApple ? .78f : 1.06f),
                                leafColor, profile.LeafOvalness);
                        }
                    }
                }
            }
            // Lightweight crown fillers close large artificial gaps above the
            // highest limbs while keeping most apples readable from outside.
            int fillerCount = detailedApple ? 31 : 24;
            for (int cluster = 0; cluster < fillerCount; cluster++)
            {
                float heightRatio = (cluster + .5f) / fillerCount;
                float angle = cluster * goldenAngle;
                float radius = Mathf.Lerp(1.6f, .55f, heightRatio) *
                    profile.FillerSpread * Between(.75f, 1.1f);
                Vector3 center = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Lerp(profile.CrownHeightLow + 1.2f,
                        profile.FillerTopHeight, heightRatio),
                    Mathf.Sin(angle) * radius);
                for (int leafIndex = 0; leafIndex < 9; leafIndex++)
                {
                    Vector3 offset = new Vector3(
                        Between(-.32f,.32f), Between(-.24f,.23f), Between(-.32f,.32f));
                    Vector3 direction = (offset + new Vector3(
                        Between(-.4f,.4f), .23f, Between(-.4f,.4f))).normalized;
                    float shade = Between(-.055f,.07f);
                    foliage.Leaf(center + offset, direction,
                        Between(detailedApple ? .15f : .20f,
                            detailedApple ? .23f : .30f),
                        Between(detailedApple ? .11f : .15f,
                            detailedApple ? .16f : .21f),
                        profile.LeafColor +
                            new Color(shade, shade, shade * .5f),
                        profile.LeafOvalness);
                }
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
            if (barkMaterial.HasProperty("_MainTex")) barkMaterial.SetTexture("_MainTex", Track(BarkTexture.Create()));
            MakeMeshObject("Merged Wood", Track(wood.Build("Wood")), barkMaterial);
            var leafMaterial = Track(VertexMaterial("Leaves", Color.white));
            if (leafMaterial.HasProperty("_Wind"))
                leafMaterial.SetFloat("_Wind", ReducedMotion ? .008f : .045f);
            if (leafMaterial.HasProperty("_AmbientLight")) leafMaterial.SetFloat("_AmbientLight", .72f);
            if (leafMaterial.HasProperty("_LeafTranslucency"))
                leafMaterial.SetFloat("_LeafTranslucency", .28f);
            if (leafMaterial.HasProperty("_Cull")) leafMaterial.SetFloat("_Cull", 0f);
            MakeMeshObject("Merged Leaves", Track(foliage.Build("Leaves")), leafMaterial);
            Mesh appleMesh = Track(profile.CreateFruitMesh());
            Color fruitColor = profile.FruitColor;
            Material appleMaterial = Track(LitMaterial("Fruit " + speciesId, fruitColor, .38f));
            if (appleMaterial.HasProperty("_MainTex"))
                appleMaterial.SetTexture("_MainTex",
                    Track(AppleSkinTexture.CreateForSpecies(speciesId)));
            // Just three shared skin variants per species: natural color variation
            // without allocating an individual material for every fruit.
            var skins = new Material[3];
            skins[0] = appleMaterial;
            for (int variant = 1; variant < skins.Length; variant++)
            {
                var tone = new Material(appleMaterial) { name = "Fruit Skin Variant " + variant };
                Color tint = variant == 1 ?
                    new Color(1f, .93f, .89f) : new Color(.93f, 1f, .93f);
                tone.color = new Color(fruitColor.r * tint.r,
                    fruitColor.g * tint.g, fruitColor.b * tint.b, fruitColor.a);
                skins[variant] = Track(tone);
            }
            Material stemMaterial = Track(LitMaterial("Stem", new Color(.28f,.19f,.095f), .12f));
            for (int i = 0; i < appleCount; i++)
            {
                Vector3 attachment = fruitAnchors[i * fruitAnchors.Count / appleCount];
                Vector3 outward = new Vector3(attachment.x, 0f, attachment.z).normalized;
                // Smaller fruit tuck underneath twig ends and leaves rather than
                // sitting like large red spheres on the outside of the crown.
                Vector3 at = attachment + Vector3.down * Between(.19f, .26f) +
                    outward * (i % 4 == 0 ? .025f : .105f) +
                    new Vector3(Between(-.05f, .05f), 0f, Between(-.05f, .05f));
                var fruit = MakeMeshObject(speciesId + " " + (i + 1), appleMesh,
                    skins[i % skins.Length]);
                fruit.transform.localPosition = at;
                // Gentle size variety with ~30% smaller apples than the early prototype.
                float fruitScale = Between(profile.FruitScaleLow, profile.FruitScaleHigh);
                fruit.transform.localScale = Vector3.one * fruitScale;
                fruit.transform.localRotation = Quaternion.Euler(Between(-10, 10), Between(0, 360), Between(-10, 10));
                fruit.AddComponent<SphereCollider>().radius = profile.HitRadius;
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
            if (label == "Merged Wood")
            {
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

        private float Between(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
