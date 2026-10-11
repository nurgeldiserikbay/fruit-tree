using System;
using System.Collections.Generic;

namespace FruitTree.Progression
{
    /// <summary>One tree's saved completion history and current harvest round.</summary>
    public sealed class TreeRoundState
    {
        private readonly HashSet<string> picked = new HashSet<string>(StringComparer.Ordinal);
        public TreeLevelDefinition Definition { get; }
        public bool CompletedOnce { get; private set; }
        public int TimesCleared { get; private set; }
        public int RoundNumber { get; private set; } = 1;
        public int PickedCount => picked.Count;
        public bool IsCleared => picked.Count == Definition.FruitCount;
        public IReadOnlyCollection<string> PickedFruitIds => picked;

        public TreeRoundState(TreeLevelDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public bool TryPick(string fruitId)
        {
            if (!Definition.ContainsFruitId(fruitId) || IsCleared || !picked.Add(fruitId)) return false;
            if (IsCleared)
            {
                CompletedOnce = true;
                TimesCleared++;
            }
            return true;
        }

        public bool TryRebloom()
        {
            if (!IsCleared) return false;
            picked.Clear();
            RoundNumber++;
            // Past clears remain a permanent achievement.
            return true;
        }

        public TreeSaveData ToSave()
        {
            var ids = new List<string>(picked);
            ids.Sort(StringComparer.Ordinal);
            return new TreeSaveData
            {
                treeId = Definition.Id,
                completedOnce = CompletedOnce,
                timesCleared = TimesCleared,
                roundNumber = RoundNumber,
                pickedFruitIds = ids
            };
        }

        internal void Restore(TreeSaveData saved)
        {
            if (saved == null || saved.treeId != Definition.Id) return;
            picked.Clear();
            if (saved.pickedFruitIds != null)
            {
                foreach (string id in saved.pickedFruitIds)
                    if (Definition.ContainsFruitId(id)) picked.Add(id);
            }
            RoundNumber = Math.Max(1, saved.roundNumber);
            TimesCleared = Math.Max(0, saved.timesCleared);
            CompletedOnce = saved.completedOnce || TimesCleared > 0;
            if (IsCleared)
            {
                // A fully picked round must be recognized after crash/restart.
                CompletedOnce = true;
                TimesCleared = Math.Max(1, TimesCleared);
            }
        }
    }
}
