using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class MusicPlaylistTests
    {
        [Test]
        public void PlaylistShipsStreamedTracks()
        {
            var playlist = Resources.Load<MusicPlaylist>("MusicPlaylist");
            Assert.IsNotNull(playlist);
            Assert.Greater(playlist.tracks.Length, 0);
            foreach (AudioClip track in playlist.tracks)
            {
                Assert.IsNotNull(track);
                Assert.AreEqual(AudioClipLoadType.Streaming, track.loadType, track.name);
            }
        }
    }
}
