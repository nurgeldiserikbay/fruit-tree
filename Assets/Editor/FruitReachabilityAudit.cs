#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Checks whether ripe fruit are visible and hittable from the actual
    /// portrait orbit camera's allowed positions. Foliage, wood and other fruit
    /// colliders block picking; transparent leaf texels are handled as in gameplay.
    /// </summary>
    public static class FruitReachabilityAudit
    {
        [MenuItem("Fruit Tree/Audit Fruit Reachability")]
        public static void Run()
        {
            var treeObject = new GameObject("Temporary Reachability Test");
            treeObject.transform.position = new Vector3(1000f, 0f, 1000f);
            try
            {
                var tree = treeObject.AddComponent<TreeGenerator>();
                tree.Generate();
                Physics.SyncTransforms();
                var blocked = FindUnreachable(tree, 48, 4);
                if (blocked.Count != 0)
                {
                    Debug.LogWarning("Fruit Tree reachability: " + blocked.Count +
                        " of " + tree.TotalApples + " apples had no clear tested view: " +
                        string.Join(", ", blocked));
                }
                else
                {
                    Debug.Log("Fruit Tree reachability: all " + tree.TotalApples +
                        " apples found from at least one sampled viewpoint.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(treeObject);
            }
        }

        /// <summary>
        /// Release gate: scans all ten loaded species from real orbit viewpoints
        /// without consuming saved progress or touching the real game scene.
        /// Run in the Editor before signing an Android App Bundle.
        /// </summary>
        [MenuItem("Fruit Tree/Audit All Ten Tree Species")]
        public static void AuditAllSpecies()
        {
            var problems = new List<string>();
            var levels = OrchardGameController.LevelDefinitions;
            for (int index = 0; index < levels.Count; index++)
            {
                var level = levels[index];
                var probe = new GameObject("Audit " + level.SpeciesId);
                probe.transform.position = new Vector3(1000f + index * 100f, 0f, 1000f);
                try
                {
                    var generated = probe.AddComponent<TreeGenerator>();
                    generated.SetAutomaticGeneration(false);
                    generated.Configure(level.SpeciesId, level.FruitCount, 122 + index * 197);
                    Physics.SyncTransforms();
                    var missed = FindUnreachable(generated, 48, 4);
                    if (missed.Count > 0)
                    {
                        problems.Add(level.SpeciesId + ": " + missed.Count + "/" +
                            level.FruitCount + " unreachable — " +
                            string.Join(", ", missed));
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }

            if (problems.Count > 0)
                throw new System.InvalidOperationException(
                    "Fruit Tree 100% harvest audit failed. Rework fruit placement: " +
                    string.Join("; ", problems));
            Debug.Log("Fruit Tree: all ten species passed the sampled fruit reachability audit.");
        }

        public static List<string> FindUnreachable(TreeGenerator tree, int angleSteps, int heightSteps)
        {
            if (tree == null) throw new System.ArgumentNullException(nameof(tree));
            if (angleSteps < 4 || heightSteps < 1)
                throw new System.ArgumentOutOfRangeException("Sampling grid too small.");
            // The old scan used arbitrary camera heights and always assumed
            // a fruit was visible if a ray could hit it. That can pass fruit
            // which are actually outside a narrow portrait screen.
            // Sample reachable orbit/zoom/focus states from the real camera.
            float[] pitches = { -20f, 13f, 38f, 58f };
            // Only distances the player can actually reach with pinch zoom.
            float[] zooms = { OrbitPickingCamera.MinimumOrbitDistance, 4f, 5.5f,
                OrbitPickingCamera.MaximumOrbitDistance };
            int totalViews = angleSteps * heightSteps * pitches.Length * zooms.Length;
            var samplePositions = new Vector3[totalViews];
            var sampleTargets = new Vector3[totalViews];
            int cursor = 0;
            Vector3 pivot = tree.transform.position;
            for (int height = 0; height < heightSteps; height++)
            {
                float focusHeight = Mathf.Lerp(OrbitPickingCamera.MinimumFocusHeight,
                    OrbitPickingCamera.MaximumFocusHeight,
                    heightSteps == 1 ? .5f : height / (float)(heightSteps - 1));
                Vector3 focus = pivot + Vector3.up * focusHeight;
                foreach (float pitch in pitches)
                {
                    foreach (float zoom in zooms)
                    {
                        for (int angle = 0; angle < angleSteps; angle++)
                        {
                            float yaw = angle * 360f / angleSteps;
                            samplePositions[cursor] = OrbitPickingCamera.OrbitPosition(
                                pivot, zoom, yaw, pitch, focusHeight);
                            sampleTargets[cursor] = focus;
                            cursor++;
                        }
                    }
                }
            }

            // A picked fruit stops blocking the ones behind it, so harvest in
            // passes: only fruit left after no further progress is truly stuck.
            var remaining = new List<Apple>();
            foreach (var apple in tree.Apples)
                if (apple != null && !apple.IsHarvested) remaining.Add(apple);
            var disabledColliders = new List<Collider>();
            try
            {
                while (remaining.Count > 0)
                {
                    Physics.SyncTransforms();
                    var reachableNow = new List<Apple>();
                    foreach (var apple in remaining)
                        if (IsReachable(apple, samplePositions, sampleTargets))
                            reachableNow.Add(apple);
                    if (reachableNow.Count == 0) break;
                    foreach (var apple in reachableNow)
                    {
                        var fruitCollider = apple.GetComponent<Collider>();
                        fruitCollider.enabled = false;
                        disabledColliders.Add(fruitCollider);
                        remaining.Remove(apple);
                    }
                }
            }
            finally
            {
                foreach (var fruitCollider in disabledColliders)
                    if (fruitCollider != null) fruitCollider.enabled = true;
                Physics.SyncTransforms();
            }

            var inaccessible = new List<string>();
            foreach (var apple in remaining)
                inaccessible.Add(apple.name + " [" + apple.StableFruitId + "]");
            return inaccessible;
        }

        private static bool IsReachable(Apple apple, Vector3[] samplePositions,
            Vector3[] sampleTargets)
        {
            Vector3 fruitPoint = apple.transform.position;
            for (int view = 0; view < samplePositions.Length; view++)
            {
                Vector3 camera = samplePositions[view];
                // Reject impossible views that would go through the ground:
                // gameplay now enforces this same physical height limit.
                if (camera.y < OrbitPickingCamera.MinimumCameraHeight)
                    continue;
                // Audit against the narrower 9:20 portrait aspect and
                // the generated 47-degree camera FOV.
                if (!OrbitPickingCamera.IsInsidePortraitView(
                        camera, sampleTargets[view], fruitPoint))
                    continue;
                Vector3 difference = fruitPoint - camera;
                float distance = difference.magnitude;
                // Gameplay only picks fruit within arm's reach of the camera.
                if (distance <= .001f || distance > OrbitPickingCamera.MaximumPickDistance) continue;
                // Match actual tap selection: scan front-to-back, skip
                // transparent leaf texels, and stop on the first blocker.
                bool previousBackfaces = Physics.queriesHitBackfaces;
                try
                {
                    Physics.queriesHitBackfaces = true;
                    RaycastHit[] hits = Physics.RaycastAll(camera,
                        difference / distance, distance + .25f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore);
                    Array.Sort(hits, (left, right) =>
                        left.distance.CompareTo(right.distance));
                    foreach (RaycastHit hit in hits)
                    {
                        if (OrbitPickingCamera.IsTransparentFoliageHit(hit))
                            continue;
                        if (hit.collider.GetComponentInParent<Apple>() == apple)
                            return true;
                        break;
                    }
                }
                finally
                {
                    Physics.queriesHitBackfaces = previousBackfaces;
                }
            }
            return false;
        }
    }
}
#endif
