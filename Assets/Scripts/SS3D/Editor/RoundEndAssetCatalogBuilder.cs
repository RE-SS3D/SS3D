#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.UI.Shell.Catalog;
using SS3D.UI.Shell.RoundEnd;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEngine.TextCore.Text;

namespace SS3D.Editor
{
    public static class RoundEndAssetCatalogBuilder
    {
        // The legacy Ode SDF has Spessman face/glyph data. Keep this correction local
        // so rebuilding the summary does not reflow every existing UI surface.
        private static void EnsureArcadeFont(List<string> missing)
        {
            const string path = "Assets/Content/Systems/UI/RoundEnd/RoundEndArcade.asset";
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(path) != null)
                return;
            Font source = UiCatalogBuilderKit.LoadRequired<Font>("Assets/Art/Font/Fonts/OdeToIdleGaming.otf", missing);
            if (source == null)
                return;
            FontAsset font = FontAsset.CreateFontAsset(source);
            font.name = "RoundEndArcade";
            string characters = "";
            for (int i = 32; i < 127; i++) characters += (char)i;
            font.TryAddCharacters(characters);
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D texture in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(texture, font);
            EditorUtility.SetDirty(font);
        }

        public static bool TryRebuildCatalog(out string error)
        {
            List<string> missing = new();
            EnsureArcadeFont(missing);
            PanelSettings settings = UiCatalogBuilderKit.LoadRequired<PanelSettings>(UiShellAssetPaths.PanelSettings, missing);
            StyleSheet tokens = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTokens, missing);
            StyleSheet typography = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTypography, missing);
            StyleSheet style = UiCatalogBuilderKit.LoadRequired<StyleSheet>(RoundEndAssetPaths.Style, missing);
            RoundEndAssetCatalog catalog = UiCatalogBuilderKit.LoadOrCreateCatalogAsset<RoundEndAssetCatalog>(RoundEndAssetPaths.Catalog);
            catalog.EditorAssign(settings, tokens, typography, style);
            return UiCatalogBuilderKit.FinalizeSave(missing, "Round-end summary visuals", catalog, out error);
        }
    }
}
#endif
