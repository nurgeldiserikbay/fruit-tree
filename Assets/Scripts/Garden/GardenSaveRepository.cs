using System;
using System.IO;
using UnityEngine;

namespace FruitTree.Garden
{
    /// <summary>The garden lives in its own small JSON file next to the orchard save.</summary>
    public sealed class GardenSaveRepository
    {
        private readonly string path;
        private readonly string temporary;
        private readonly string backup;

        public GardenSaveRepository(string directory)
        {
            path = Path.Combine(directory, "fruit-tree-garden.json");
            temporary = path + ".tmp";
            backup = path + ".bak";
        }

        public bool Exists => File.Exists(path) || File.Exists(backup);

        public GardenSaveData Load()
        {
            return TryRead(path) ?? TryRead(temporary) ?? TryRead(backup);
        }

        public bool Save(GardenSaveData save)
        {
            if (save == null || save.plots == null || save.seeds == null) return false;
            try
            {
                string folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(temporary, JsonUtility.ToJson(save, true));
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporary, path, backup);
                        return true;
                    }
                    catch (PlatformNotSupportedException) { }
                    catch (IOException) { }
                    File.Copy(path, backup, true);
                    File.Delete(path);
                }
                File.Move(temporary, path);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Fruit Tree: garden was not saved: " + error.Message);
                return false;
            }
        }

        private static GardenSaveData TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var save = JsonUtility.FromJson<GardenSaveData>(File.ReadAllText(file));
                return save != null && save.schemaVersion == 1 ? save : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
