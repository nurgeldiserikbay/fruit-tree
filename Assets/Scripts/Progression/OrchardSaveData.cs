using System;
using System.Collections.Generic;

namespace FruitTree.Progression
{
    /// <summary>Unity JsonUtility compatible, versioned DTOs; no Dictionary fields.</summary>
    [Serializable]
    public sealed class OrchardSaveData
    {
        public int schemaVersion = 1;
        public string activeTreeId = "tree_001";
        public List<TreeSaveData> trees = new List<TreeSaveData>();
    }

    [Serializable]
    public sealed class TreeSaveData
    {
        public string treeId;
        public bool completedOnce;
        public int timesCleared;
        public int roundNumber = 1;
        public List<string> pickedFruitIds = new List<string>();
    }
}
