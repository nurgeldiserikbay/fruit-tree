#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Any track placed in Assets/Audio joins the game's playlist. Tracks stream
    /// from disk, so long music does not sit in phone memory.
    /// </summary>
    public sealed class MusicPlaylistBuilder : AssetPostprocessor
    {
        private const string Folder = "Assets/Audio";
        private const string PlaylistPath = "Assets/Resources/MusicPlaylist.asset";
        private static bool rebuildQueued;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder + "/", StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .45f;
            importer.defaultSampleSettings = settings;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            bool changed = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
                .Any(path => path.StartsWith(Folder + "/", StringComparison.Ordinal));
            if (!changed || rebuildQueued) return;
            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                rebuildQueued = false;
                Rebuild();
            };
        }

        [MenuItem("Fruit Tree/Rebuild Music Playlist")]
        public static void Rebuild()
        {
            AudioClip[] clips = AssetDatabase.IsValidFolder(Folder)
                ? AssetDatabase.FindAssets("t:AudioClip", new[] { Folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                    .Where(clip => clip != null)
                    .ToArray()
                : new AudioClip[0];

            var playlist = AssetDatabase.LoadAssetAtPath<MusicPlaylist>(PlaylistPath);
            if (playlist == null)
            {
                playlist = ScriptableObject.CreateInstance<MusicPlaylist>();
                AssetDatabase.CreateAsset(playlist, PlaylistPath);
            }
            if (playlist.tracks != null && playlist.tracks.SequenceEqual(clips)) return;
            playlist.tracks = clips;
            EditorUtility.SetDirty(playlist);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
