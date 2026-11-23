using UnityEngine;

namespace FruitTree
{
    /// <summary>Background tracks from Assets/Audio; rebuilt automatically in the Editor.</summary>
    public sealed class MusicPlaylist : ScriptableObject
    {
        public AudioClip[] tracks = new AudioClip[0];
    }
}
