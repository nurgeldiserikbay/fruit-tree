using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Generated scene layout revision, independent from saved orchard data.
    /// Existing custom scenes are not overwritten automatically on source pull.
    /// </summary>
    public sealed class FruitTreeSceneVersion : MonoBehaviour
    {
        public const int Current = 7;

        [SerializeField] private int generatedVersion;
        public int GeneratedVersion => generatedVersion;
        public bool IsCurrent => generatedVersion == Current;

        public void StampGeneratedScene()
        {
            generatedVersion = Current;
        }
    }
}
