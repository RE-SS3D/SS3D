#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.UI.Shell.Catalog;
using SS3D.UI.Shell.MainMenu;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    public static class MainMenuAssetCatalogBuilder
    {
        /// <summary>BC7 keeps the art's gradients at a quarter of the size: about 2 MB in a build instead of 8.</summary>
        private static void ConfigureWallpaper(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled
                && importer.textureCompression == TextureImporterCompression.CompressedHQ) return;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        /// <summary>Menu music streams (Space Theme alone decodes to ~30 MB) and stays out of memory until played.</summary>
        private static void ConfigureMusic(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming && settings.compressionFormat == AudioCompressionFormat.Vorbis
                && !settings.preloadAudioData && Mathf.Approximately(settings.quality, 0.7f)) return;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static AudioClip[] LoadMusic(List<string> missing)
        {
            List<AudioClip> clips = new();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MainMenuAssetPaths.MusicFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.Array.IndexOf(MainMenuAssetPaths.ExcludedMusic, System.IO.Path.GetFileNameWithoutExtension(path)) >= 0) continue;
                ConfigureMusic(path);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }

            if (clips.Count == 0) missing.Add(MainMenuAssetPaths.MusicFolder + " (no music)");
            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips.ToArray();
        }

        public static bool TryRebuildCatalog(out string error)
        {
            List<string> missing = new();
            PanelSettings settings = UiCatalogBuilderKit.LoadRequired<PanelSettings>(UiShellAssetPaths.PanelSettings, missing);
            StyleSheet tokens = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTokens, missing);
            StyleSheet typography = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTypography, missing);
            VisualTreeAsset template = UiCatalogBuilderKit.LoadRequired<VisualTreeAsset>(MainMenuAssetPaths.Template, missing);
            StyleSheet style = UiCatalogBuilderKit.LoadRequired<StyleSheet>(MainMenuAssetPaths.Style, missing);
            Texture2D logo = UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.Logo, missing);
            ConfigureWallpaper(MainMenuAssetPaths.Wallpaper);
            Texture2D wallpaper = UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.Wallpaper, missing);
            Texture2D musicIcon = UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.MusicIcon, missing);
            AudioClip[] music = LoadMusic(missing);
            MainMenuAssetCatalog catalog = UiCatalogBuilderKit.LoadOrCreateCatalogAsset<MainMenuAssetCatalog>(MainMenuAssetPaths.Catalog);
            catalog.EditorAssign(settings, tokens, typography, template, style, logo, wallpaper, musicIcon, music);
            return UiCatalogBuilderKit.FinalizeSave(missing, "Main menu visuals", catalog, out error);
        }
    }
}
#endif
