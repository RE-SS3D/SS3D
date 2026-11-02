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
        private static void ConfigureWallpaper(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled
                && importer.textureCompression == TextureImporterCompression.Uncompressed) return;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
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
            ConfigureWallpaper(MainMenuAssetPaths.WallpaperFolder + "FloatingSpace2.png");
            ConfigureWallpaper(MainMenuAssetPaths.WallpaperFolder + "FloatingSpace3.png");
            List<string> missing = new();
            PanelSettings settings = UiCatalogBuilderKit.LoadRequired<PanelSettings>(UiShellAssetPaths.PanelSettings, missing);
            StyleSheet tokens = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTokens, missing);
            StyleSheet typography = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTypography, missing);
            VisualTreeAsset template = UiCatalogBuilderKit.LoadRequired<VisualTreeAsset>(MainMenuAssetPaths.Template, missing);
            StyleSheet style = UiCatalogBuilderKit.LoadRequired<StyleSheet>(MainMenuAssetPaths.Style, missing);
            Texture2D logo = UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.Logo, missing);
            Texture2D[] wallpapers = {
                UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.WallpaperFolder + "FloatingSpace2.png", missing),
                UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.WallpaperFolder + "FloatingSpace3.png", missing),
            };
            Texture2D musicIcon = UiCatalogBuilderKit.LoadRequired<Texture2D>(MainMenuAssetPaths.MusicIcon, missing);
            AudioClip[] music = LoadMusic(missing);
            MainMenuAssetCatalog catalog = UiCatalogBuilderKit.LoadOrCreateCatalogAsset<MainMenuAssetCatalog>(MainMenuAssetPaths.Catalog);
            catalog.EditorAssign(settings, tokens, typography, template, style, logo, wallpapers, musicIcon, music);
            return UiCatalogBuilderKit.FinalizeSave(missing, "Main menu visuals", catalog, out error);
        }
    }
}
#endif
