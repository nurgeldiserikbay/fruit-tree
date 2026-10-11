using System;
using System.Collections.Generic;

namespace FruitTree.Progression
{
    /// <summary>
    /// Pure, offline progression rules. Does not instantiate Unity scene objects.
    /// A later scene presenter will map stable fruit IDs to pickable objects.
    /// </summary>
    public sealed class OrchardProgressionService
    {
        private readonly List<TreeLevelDefinition> definitions;
        private readonly Dictionary<string, TreeRoundState> states =
            new Dictionary<string, TreeRoundState>(StringComparer.Ordinal);

        public string ActiveTreeId { get; private set; }
        public IReadOnlyList<TreeLevelDefinition> Definitions => definitions;

        public OrchardProgressionService(IReadOnlyList<TreeLevelDefinition> catalog,
            OrchardSaveData saved = null)
        {
            if (catalog == null || catalog.Count == 0)
                throw new ArgumentException("The catalog must contain at least one tree.", nameof(catalog));
            if (saved != null && saved.schemaVersion != 1)
                throw new ArgumentException("Unsupported save schema.", nameof(saved));

            definitions = new List<TreeLevelDefinition>(catalog);
            foreach (var tree in definitions)
            {
                if (tree == null || states.ContainsKey(tree.Id))
                    throw new ArgumentException("Tree catalog has null or duplicate IDs.", nameof(catalog));
                states.Add(tree.Id, new TreeRoundState(tree));
            }
            if (saved != null && saved.trees != null)
            {
                foreach (var treeSave in saved.trees)
                {
                    if (treeSave != null && treeSave.treeId != null &&
                        states.TryGetValue(treeSave.treeId, out var state))
                        state.Restore(treeSave);
                }
            }

            ActiveTreeId = definitions[0].Id;
            if (saved != null && IsUnlocked(saved.activeTreeId))
                ActiveTreeId = saved.activeTreeId;
        }

        public TreeRoundState GetTree(string treeId)
        {
            if (treeId == null || !states.TryGetValue(treeId, out var state))
                return null;
            return state;
        }

        public bool IsUnlocked(string treeId)
        {
            if (string.IsNullOrEmpty(treeId)) return false;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].Id != treeId) continue;
                return i == 0 || states[definitions[i - 1].Id].CompletedOnce;
            }
            return false;
        }

        public bool SelectTree(string treeId)
        {
            if (!IsUnlocked(treeId)) return false;
            ActiveTreeId = treeId;
            return true;
        }

        public bool TryPickActive(string fruitId)
        {
            return states[ActiveTreeId].TryPick(fruitId);
        }

        public bool TryRebloomActive()
        {
            return states[ActiveTreeId].TryRebloom();
        }

        public string NextUnlockedTreeId()
        {
            for (int i = 0; i < definitions.Count - 1; i++)
                if (definitions[i].Id == ActiveTreeId &&
                    IsUnlocked(definitions[i + 1].Id))
                    return definitions[i + 1].Id;
            return null;
        }

        public OrchardSaveData ToSave()
        {
            var saved = new OrchardSaveData { activeTreeId = ActiveTreeId };
            foreach (var tree in definitions)
                saved.trees.Add(states[tree.Id].ToSave());
            return saved;
        }
    }
}
