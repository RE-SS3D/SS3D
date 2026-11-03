using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace SS3D.UI.Shell.Fonts
{
    /// <summary>
    /// The UI Toolkit font assets are static atlases baked with <see cref="Characters"/> (SS3D → Build →
    /// Bake Static UI Fonts), so drawing text never rewrites them and git stays clean. Text outside that
    /// set (player chat in other scripts) falls back to a dynamic atlas made from the same source font at
    /// runtime: never saved, removed again when play ends.
    /// </summary>
    public sealed class UiFontFallbacks : ScriptableObject
    {
        public const string ResourceName = "UiFontFallbacks";

        /// <summary>
        /// Baked into every static UI atlas: printable ASCII, Latin-1 and the symbols the game's own UI
        /// strings use. Add a character here (and re-bake) when UI text starts using it.
        /// </summary>
        public static readonly string Characters = BuildCharacters();

        [Serializable]
        public struct Entry
        {
            public FontAsset Asset;
            public Font Source;
            /// <summary>The <see cref="Characters"/> this asset was baked with; a re-bake skips it while they match.</summary>
            public string BakedCharacters;
        }

        [SerializeField]
        private List<Entry> _entries = new();

        private static readonly List<(FontAsset Owner, FontAsset Fallback)> Attached = new();

        public List<Entry> Entries => _entries;

        private static string BuildCharacters()
        {
            System.Text.StringBuilder characters = new();
            for (char c = (char)0x20; c <= 0x7E; c++)
                characters.Append(c);
            for (char c = (char)0xA0; c <= 0xFF; c++)
                characters.Append(c);
            characters.Append("–—‘’“”•…‹›€₂←→↑↓↗⇄−≤≥●○■□✓✕×");
            return characters.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Attach()
        {
            Detach();
            UiFontFallbacks catalog = Resources.Load<UiFontFallbacks>(ResourceName);
            if (catalog == null)
                return;

            foreach (Entry entry in catalog._entries)
            {
                if (entry.Asset == null || entry.Source == null || entry.Asset.atlasPopulationMode != AtlasPopulationMode.Static)
                    continue;

                FontAsset fallback = FontAsset.CreateFontAsset(entry.Source, (int)entry.Asset.faceInfo.pointSize,
                    entry.Asset.atlasPadding, entry.Asset.atlasRenderMode, entry.Asset.atlasWidth, entry.Asset.atlasHeight,
                    AtlasPopulationMode.Dynamic, true);
                if (fallback == null)
                    continue;

                fallback.name = entry.Asset.name + " (runtime fallback)";
                fallback.hideFlags = HideFlags.DontSave;
                entry.Asset.fallbackFontAssetTable ??= new List<FontAsset>();
                entry.Asset.fallbackFontAssetTable.Insert(0, fallback);
                Attached.Add((entry.Asset, fallback));
            }

            Application.quitting -= Detach;
            Application.quitting += Detach;
        }

        /// <summary>Takes the runtime fallbacks off the assets, so nothing points at them after play.</summary>
        private static void Detach()
        {
            foreach ((FontAsset owner, FontAsset fallback) in Attached)
            {
                if (owner != null)
                    owner.fallbackFontAssetTable?.Remove(fallback);
                if (fallback != null)
                    Destroy(fallback);
            }

            Attached.Clear();
        }
    }
}
