#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Checks that ripe fruit can be selected from viewpoints around the tree.
    /// Leaf cards intentionally have no colliders; real branch and apple colliders do.
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
                Object.DestroyImmediate(treeObject);
            }
        }

        /// <summary>
        /// Release gate: scans all ten loaded species from sampled viewpoints
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
                finally { Object.DestroyImmediate(probe); }
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
            var inaccessible = new List<string>();
            Physics.SyncTransforms();
            foreach (var apple in tree.Apples)
            {
                if (apple == null || apple.IsHarvested) continue;
                Vector3 target = apple.transform.position;
                bool reachable = false;
                for (int y = 0; y < heightSteps && !reachable; y++)
                {
                    float cameraY = Mathf.Lerp(1.25f, 4.8f, heightSteps == 1 ? .5f :
                        y / (float)(heightSteps - 1));
                    for (int a = 0; a < angleSteps; a++)
                    {
                        float angle = a * Mathf.PI * 2f / angleSteps;
                        var camera = tree.transform.position + new Vector3(
                            Mathf.Cos(angle) * 9f, cameraY, Mathf.Sin(angle) * 9f);
                        Vector3 difference = target - camera;
                        float distance = difference.magnitude;
                        if (distance <= .001f) continue;
                        if (!Physics.Raycast(camera, difference / distance, out RaycastHit hit,
                                distance + .25f, Physics.DefaultRaycastLayers,
                                QueryTriggerInteraction.Ignore))
                            continue;
                        if (hit.collider.GetComponentInParent<Apple>() == apple)
                        {
                            reachable = true;
                            break;
                        }
                    }
                }
                if (!reachable) inaccessible.Add(apple.name);
            }
            return inaccessible;
        }
    }
}
#endif
