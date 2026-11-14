#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SS3D.UI.Lobby;
using SS3D.Systems.Inventory.Containers;
using UnityEditor;
using UnityEngine;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="LobbyAssetCatalog"/> from <see cref="LobbyAssetPaths"/>.
    /// Menu: use <see cref="UiCatalogRebuildAll"/>.
    /// </summary>
    public static class LobbyAssetCatalogBuilder
    {
        /// <summary>Hair prefabs are auto-discovered from this folder, sorted alphabetically. Index 0 is the "None" sentinel (null).</summary>
        private const string HairPrefabRoot = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanHair/Head";

        /// <summary>Beard/facial-hair prefabs are auto-discovered from this folder.</summary>
        private const string BeardPrefabRoot = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanHair/Face";

        /// <summary>
        /// Default hair colour palette. These are authored here rather than as assets because
        /// they are simple named swatches and adding a new colour only needs a code change + rebuild.
        /// </summary>
        private static readonly (string Id, Color Color)[] DefaultHairColors =
        {
            ("black",       new Color(0.07f, 0.07f, 0.07f)),
            ("dark-brown",  new Color(0.25f, 0.13f, 0.07f)),
            ("brown",       new Color(0.47f, 0.26f, 0.13f)),
            ("auburn",      new Color(0.55f, 0.20f, 0.09f)),
            ("light-brown", new Color(0.62f, 0.42f, 0.22f)),
            ("dark-blonde", new Color(0.72f, 0.57f, 0.26f)),
            ("blonde",      new Color(0.93f, 0.82f, 0.49f)),
            ("platinum",    new Color(0.96f, 0.96f, 0.92f)),
            ("grey",        new Color(0.55f, 0.55f, 0.55f)),
            ("white",       new Color(0.95f, 0.95f, 0.95f)),
            ("red",         new Color(0.72f, 0.15f, 0.08f)),
            ("ginger",      new Color(0.87f, 0.39f, 0.10f)),
            ("strawberry",  new Color(0.96f, 0.64f, 0.50f)),
            ("pink",        new Color(0.97f, 0.63f, 0.74f)),
            ("purple",      new Color(0.50f, 0.17f, 0.72f)),
            ("blue",        new Color(0.14f, 0.32f, 0.87f)),
            ("green",       new Color(0.10f, 0.60f, 0.25f)),
            ("teal",        new Color(0.08f, 0.62f, 0.62f)),
        };

        private static readonly (string Id, string FileName)[] DepartmentIconFiles =
        {
            ("command", "OfficeBuilding.png"),
            ("security", "ShieldCheck.png"),
            ("engineering", "Cog.png"),
            ("medical", "Plus.png"),
            ("science", "Beaker.png"),
            ("cargo", "Truck.png"),
            ("service", "Cake.png"),
            ("civilian", "UserGroup.png"),
        };

        /// <summary>BatchMode: <c>-executeMethod SS3D.Editor.LobbyAssetCatalogBuilder.RebuildCatalogBatch</c></summary>
        public static void RebuildCatalogBatch()
        {
            if (!TryRebuildCatalog(out string error))
            {
                Debug.LogError(error);
                if (UnityEngine.Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            Debug.Log($"[LobbyAssetCatalogBuilder] Rebuilt catalog at {LobbyAssetPaths.CatalogAssetPath}");
            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool TryRebuildCatalog(out string error)
        {
            error = null;
            List<string> missing = new();

            StyleSheet lobbyStyle = LoadRequired<StyleSheet>(LobbyAssetPaths.StyleSheet, missing);
            Sprite preview = LoadRequiredSprite(LobbyAssetPaths.PreviewPlaceholder, missing);
            Sprite banner = LoadRequiredSprite(LobbyAssetPaths.ServerInfoBanner, missing);
            // Heroicons are Default Texture2D (not Sprite) — load as textures for UITK backgrounds.
            Texture2D chevron = LoadRequiredTexture(LobbyAssetPaths.ChevronDownIcon, missing);
            GameObject previewHuman = LoadRequired<GameObject>(LobbyAssetPaths.PreviewHumanPrefab, missing);
            CharacterNameList characterNames = LoadRequired<CharacterNameList>(CharacterNameList.AssetPath, missing);

            List<LobbyNamedSprite> jobIcons = new();
            if (Directory.Exists(LobbyAssetPaths.JobIconRoot))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { LobbyAssetPaths.JobIconRoot.TrimEnd('/') });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                    {
                        missing.Add($"Sprite: {path}");
                        continue;
                    }

                    string id = Path.GetFileNameWithoutExtension(path);
                    jobIcons.Add(new LobbyNamedSprite(id, sprite));
                }
            }
            else
            {
                missing.Add(LobbyAssetPaths.JobIconRoot);
            }

            List<LobbyNamedTexture> departmentIcons = new();
            foreach ((string id, string fileName) in DepartmentIconFiles)
            {
                Texture2D texture = LoadRequiredTexture(LobbyAssetPaths.HeroiconsOutlineRoot + fileName, missing);
                if (texture != null)
                {
                    departmentIcons.Add(new LobbyNamedTexture(id, texture));
                }
            }

            List<LobbyNamedSprite> loadoutThumbs = new();
            Sprite securityThumb = LoadRequiredSprite(LobbyAssetPaths.PreviewPlaceholder, missing);
            if (securityThumb != null)
            {
                loadoutThumbs.Add(new LobbyNamedSprite("PnSecurity", securityThumb));
            }

            Sprite janitorThumb = LoadRequiredSprite(LobbyAssetPaths.LoadoutJanitor, missing);
            if (janitorThumb != null)
            {
                loadoutThumbs.Add(new LobbyNamedSprite("PnJanitor", janitorThumb));
            }

            // Hair: index 0 is the "none" sentinel (null prefab), then alpha-sorted discoveries.
            List<LobbyNamedPrefab> hairStyles = new() { new LobbyNamedPrefab("none", null) };
            hairStyles.AddRange(DiscoverPrefabs(HairPrefabRoot));

            List<LobbyNamedPrefab> beardStyles = new() { new LobbyNamedPrefab("none", null) };
            beardStyles.AddRange(DiscoverPrefabs(BeardPrefabRoot));

            List<Color> hairColors = new();
            foreach ((string _, Color c) in DefaultHairColors)
            {
                hairColors.Add(c);
            }

            const string clothingRoot = "Assets/Content/WorldObjects/Items/Clothing/";
            List<LobbyPreviewOutfit> previewOutfits = new();
            foreach ((string id, string jumpsuitName, string shoesName) in new (string, string, string)[]
            {
                ("civilian", "JumpsuitGrey", "ShoeHiTops"),
                ("engineering", "JumpsuitEngineer", "ShoeJackboots"),
                ("botany", "JumpsuitBotany", "ShoeHiTops"),
                ("security", "JumpsuitSecurity", "ShoeJackboots"),
            })
            {
                GameObject jumpsuit = LoadRequired<GameObject>(clothingRoot + jumpsuitName + ".prefab", missing);
                GameObject shoes = LoadRequired<GameObject>(clothingRoot + shoesName + ".prefab", missing);
                if (jumpsuit != null && shoes != null)
                {
                    Cloth suitCloth = jumpsuit.GetComponent<Cloth>();
                    Cloth shoeCloth = shoes.GetComponent<Cloth>();
                    if (suitCloth == null || shoeCloth == null)
                    {
                        missing.Add("Cloth data for preview outfit: " + id);
                    }
                    else if (suitCloth.GetClothMesh(ClothType.Jumpsuit) == null
                        || shoeCloth.GetClothMesh(ClothType.ShoeLeft) == null
                        || shoeCloth.GetClothMesh(ClothType.ShoeRight) == null)
                    {
                        missing.Add("Worn meshes for preview outfit: " + id);
                    }
                    previewOutfits.Add(new LobbyPreviewOutfit { Id = id, Jumpsuit = jumpsuit, Shoes = shoes });
                }
            }

            if (missing.Count > 0)
            {
                error = "Lobby asset catalog rebuild failed. Missing assets:\n- "
                    + string.Join("\n- ", missing);
                return false;
            }

            string directory = Path.GetDirectoryName(LobbyAssetPaths.CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            LobbyAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LobbyAssetCatalog>(LobbyAssetPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LobbyAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, LobbyAssetPaths.CatalogAssetPath);
            }

            catalog.EditorAssign(
                lobbyStyle,
                preview,
                banner,
                chevron,
                previewHuman,
                jobIcons,
                departmentIcons,
                loadoutThumbs,
                hairStyles,
                beardStyles,
                new List<LobbyNamedPrefab>(DiscoverPrefabs("Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanHair/Eyebrows")),
                hairColors,
                previewOutfits,
                characterNames);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static T LoadRequired<T>(string path, List<string> missing) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                missing.Add($"{typeof(T).Name}: {path}");
            }

            return asset;
        }

        private static Sprite LoadRequiredSprite(string path, List<string> missing)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                missing.Add($"Sprite: {path}");
            }

            return sprite;
        }

        private static Texture2D LoadRequiredTexture(string path, List<string> missing)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                missing.Add($"Texture2D: {path}");
            }

            return texture;
        }

        private static List<LobbyNamedPrefab> DiscoverPrefabs(string folder)
        {
            List<LobbyNamedPrefab> list = new();
            if (!Directory.Exists(folder))
            {
                return list;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            List<(string id, GameObject prefab)> found = new();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                string id = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                found.Add((id, prefab));
            }

            found.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach ((string id, GameObject prefab) in found)
            {
                list.Add(new LobbyNamedPrefab(id, prefab));
            }

            return list;
        }
    }
}
#endif
