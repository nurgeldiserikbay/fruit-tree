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
                    File.Copy(path, backup, true);
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
                    save.trees == null || string.IsNullOrEmpty(save.activeTreeId))
                    return null;
                return save;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
