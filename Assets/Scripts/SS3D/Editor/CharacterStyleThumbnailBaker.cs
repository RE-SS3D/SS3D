using System.Collections.Generic;
using System.IO;
using SS3D.Systems.Entities.Character;
using SS3D.UI.Lobby;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Bakes the character window's style thumbnails (lobby effort Phase 5): each hair, facial hair and eyebrow style in
    /// the lobby catalog, worn by the preview booth's dummy and cropped around the head, saved as a PNG under
    /// <see cref="LobbyAssetPaths.StyleThumbnailRoot"/>. Re-run after adding a style, then the catalog rebuild picks
    /// the PNGs up. Edit mode only: it drives a booth with no frame loop and destroys it afterwards.
    /// </summary>
    public static class CharacterStyleThumbnailBaker
    {
        private const int Size = 160;

        /// <summary>The style tiles' fill (--lb-inset), so a thumbnail sits flush in its tile.</summary>
        private static readonly Color Background = new(0.1647059f, 0.1647059f, 0.1803922f, 1f);

        // Crop height (fraction of the frame) and lift above the head bone, per grid.
        private const float HeadCrop = 0.25f;
        private const float HeadLift = 0.15f;
        private const float BeardCrop = 0.17f;
        private const float BeardLift = 0.02f;
        private const float BrowCrop = 0.085f;
        private const float BrowLift = 0.1f;

        // Neutral look: a mid skin tone, light-brown hair (shows the shape), dark-brown brows (read on skin).
        private const int SkinIndex = 2;
        private const int EyeIndex = 0;
        private const int HairColorIndex = 4;
        private const int BrowColorIndex = 1;

        [MenuItem("SS3D/Data/Bake Character Style Thumbnails")]
        public static void BakeMenu()
        {
            string result = Bake();
            Debug.Log("[StyleThumbnails] " + result);
        }

        /// <summary>Bakes every thumbnail and rebuilds the lobby catalog; returns a one-line summary.</summary>
        public static string Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return "Exit Play Mode first.";
            }

            LobbyAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<LobbyAssetCatalog>(LobbyAssetPaths.CatalogAssetPath);
            if (catalog == null || catalog.PreviewHumanPrefab == null)
            {
                return "No lobby catalog or preview human prefab.";
            }

            Directory.CreateDirectory(LobbyAssetPaths.StyleThumbnailRoot);
            GameObject host = new("StyleThumbnailBooth") { hideFlags = HideFlags.DontSave };
            RenderTexture crop = new(Size, Size, 0, RenderTextureFormat.ARGB32) { name = "StyleThumbnailRT" };
            Texture2D readback = new(Size, Size, TextureFormat.RGBA32, false);
            List<string> written = new();
            CharacterPreviewBooth booth = null;
            try
            {
                crop.Create();
                booth = host.AddComponent<CharacterPreviewBooth>();
                booth.Initialize(catalog.PreviewHumanPrefab);
                Camera camera = host.GetComponentInChildren<Camera>(true);
                camera.backgroundColor = Background;
                booth.SetAngleIndex(0);
                booth.ApplyOutfit(new List<SS3D.Systems.Inventory.Containers.Cloth>());
                booth.ApplyBodyMorphs(0f, 0f, 0f, 0f, 0.5f, 0.5f);

                Color skin = catalog.GetSkinColor(SkinIndex);
                Color eyes = catalog.GetEyeColor(EyeIndex);
                Color hair = catalog.GetHairColor(HairColorIndex);
                Color brows = catalog.GetHairColor(BrowColorIndex);
                GameObject defaultBrows = catalog.GetEyebrowStyle("humaneyebrowsdefault");

                booth.ApplyFace(skin, eyes, defaultBrows, brows);
                for (int i = 0; i < catalog.HairStyles.Count; i++)
                {
                    booth.ApplyStyle(catalog.GetHairStyle(i), null, hair, hair);
                    Write(booth, crop, readback, "hair", catalog.HairStyles[i].Id, HeadCrop, HeadLift, written);
                }

                for (int i = 0; i < catalog.BeardStyles.Count; i++)
                {
                    booth.ApplyStyle(null, catalog.GetBeardStyle(i), hair, hair);
                    Write(booth, crop, readback, "beard", catalog.BeardStyles[i].Id, BeardCrop, BeardLift, written);
                }

                booth.ApplyStyle(null, null, hair, hair);
                booth.ApplyFace(skin, eyes, null, brows);
                Write(booth, crop, readback, "eyebrows", "none", BrowCrop, BrowLift, written);
                foreach (LobbyNamedPrefab style in catalog.EyebrowStyles)
                {
                    booth.ApplyFace(skin, eyes, style.Prefab, brows);
                    Write(booth, crop, readback, "eyebrows", CharacterAppearance.NormalizeEyebrowStyle(style.Id), BrowCrop, BrowLift, written);
                }
            }
            finally
            {
                RenderTexture texture = booth != null ? booth.PreviewTexture : null;
                Object.DestroyImmediate(host);
                if (texture != null)
                {
                    texture.Release();
                    Object.DestroyImmediate(texture);
                }

                crop.Release();
                Object.DestroyImmediate(crop);
                Object.DestroyImmediate(readback);
            }

            AssetDatabase.Refresh();
            foreach (string path in written)
            {
                ConfigureImporter(path);
            }

            bool rebuilt = LobbyAssetCatalogBuilder.TryRebuildCatalog(out string error);
            return $"Baked {written.Count} thumbnails into {LobbyAssetPaths.StyleThumbnailRoot}; catalog rebuilt: {rebuilt} {error}";
        }

        private static void Write(CharacterPreviewBooth booth, RenderTexture crop, Texture2D readback, string kind, string id,
            float cropHeight, float lift, List<string> written)
        {
            if (!booth.CaptureHeadCrop(crop, cropHeight, lift, true))
            {
                Debug.LogWarning($"[StyleThumbnails] Could not capture {kind} {id}.");
                return;
            }

            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = crop;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }

            string path = LobbyAssetPaths.StyleThumbnailRoot + LobbyAssetCatalog.StyleThumbnailKey(kind, id) + ".png";
            File.WriteAllBytes(path, readback.EncodeToPNG());
            written.Add(path);
        }

        private static void ConfigureImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
