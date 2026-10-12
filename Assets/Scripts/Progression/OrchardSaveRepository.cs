using System;
using System.IO;
using UnityEngine;

namespace FruitTree.Progression
{
    /// <summary>Local-only saves with a temporary file and recoverable previous copy.</summary>
    public sealed class OrchardSaveRepository
    {
        private readonly string path;
        private readonly string backup;
        private readonly string temporary;

        public OrchardSaveRepository(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("A save directory is required.", nameof(directory));
            path = Path.Combine(directory, "fruit-tree-orchard.json");
            backup = path + ".bak";
            temporary = path + ".tmp";
        }

        public OrchardSaveData Load()
        {
            var main = TryRead(path);
            if (main != null) return main;
            // A crash between writing the complete temporary file and
            // replacing the primary save can leave a newer valid snapshot.
            var interrupted = TryRead(temporary);
            if (interrupted != null)
            {
                Debug.LogWarning("Fruit Tree: recovered complete interrupted save.");
                return interrupted;
            }
            var recovered = TryRead(backup);
            if (recovered != null)
            {
                Debug.LogWarning("Fruit Tree: restored prior orchard progress backup.");
                return recovered;
            }
            return null;
        }

        public bool Save(OrchardSaveData save)
        {
            if (save == null || save.schemaVersion != 1) return false;
            try
            {
                string folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string serialized = JsonUtility.ToJson(save, true);
                File.WriteAllText(temporary, serialized);
                if (File.Exists(path) && TryRead(path) != null)
                {
                    // On most Android file systems this replaces the existing
                    // file atomically while keeping the previous valid version.
                    // Do not destroy the last known-good backup first.
                    try
                    {
                        File.Replace(temporary, path, backup);
                        return true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Some device runtimes do not implement File.Replace.
                    }
                    catch (IOException)
                    {
                        // Conservative fallback: retain a valid old backup.
                    }
                    File.Copy(path, backup, true);
                }
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Fruit Tree save failed: " + ex.Message);
                return false;
            }
        }

        private static OrchardSaveData TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var content = File.ReadAllText(file);
                var save = JsonUtility.FromJson<OrchardSaveData>(content);
                if (save == null || save.schemaVersion != 1 ||
                    save.trees == null || save.trees.Count == 0 ||
                    string.IsNullOrEmpty(save.activeTreeId))
                    return null;

                // A syntactically valid but truncated JSON object is not a
                // recoverable game. Reject incomplete tree entries so the
                // valid previous backup can restore the player's collection.
                var ids = new System.Collections.Generic.HashSet<string>(
                    StringComparer.Ordinal);
                bool containsActive = false;
                foreach (var tree in save.trees)
                {
                    if (tree == null || string.IsNullOrEmpty(tree.treeId) ||
                        tree.pickedFruitIds == null || tree.roundNumber < 1 ||
                        tree.timesCleared < 0 || !ids.Add(tree.treeId))
                        return null;
                    if (tree.treeId == save.activeTreeId) containsActive = true;
                }
                if (!containsActive) return null;
                return save;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
