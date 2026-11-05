using SS3D.Attributes;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetAudit
{

    public static class AssetAuditUtilities
    {
        public const string PrefabRootPath = "Assets/Content";
        public const string SceneRootPath = "Assets/Content/Scenes";
        public const string GenericRootPath = "Assets";
        private const string PrefabSearchTerm = "t:prefab";
        private const string SceneSearchTerm = "t:scene";
        private const string TileObjectSoSearchTerm = "t:TileObjectSo";
        private const string ItemObjectSoSearchTerm = "t:ItemObjectSo";

        // --- Asset/file organization taxonomy checks -------------------------------------------
        // See Documents/architecture/2026-07_asset-file-structure-taxonomy.md for the full audit
        // and rationale. Grandfather lists below are pre-existing, already-tracked violations of
        // the rules; they exist so these tests catch NEW drift without failing on old debt. Shrink
        // a grandfather list as its matching Phase 1/2 checklist item in the taxonomy doc ships —
        // never grow one to make a new violation pass.

        private static readonly string[] RawArtExtensions =
        {
            ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".bmp", ".gif",
            ".svg", ".wav", ".mp3", ".ogg", ".fbx", ".blend", ".ttf", ".otf",
            ".webm", ".mp4", ".mov", ".ogv",
        };

        // Third-party/vendored trees are exempt from this fork's taxonomy entirely — never
        // reorganize them to match our conventions (see taxonomy doc, target taxonomy table).
        private static readonly string[] VendoredPathPrefixes =
        {
            "Assets/FishNet/",
            "Assets/Scripts/External/",
        };

        // Raw art file types currently living under Assets/Content/ instead of Assets/Art/.
        // Taxonomy doc Phase 1 leftover (splatter.png).
        private static readonly string[] ContentRawArtGrandfather =
        {
            "Assets/Content/WorldObjects/World/VFX/Health/splatter.png",
        };

        // Icon image files (svg, or png with "icon" in the path) living outside
        // Assets/Art/Icons/. Phase 1 icons consolidated; InteractionIcons + CloseIcon +
        // TMP sprite sheets remain for Phase 2.
        private static readonly string[] ScatteredIconGrandfather =
        {
            "Assets/Art/Graphics/UI/Interactions/InteractionIcons/",
            "Assets/Art/Graphics/UI/Interactions/RadialMenu/CloseIcon.png",
            "Assets/Art/Font/SpriteAssets/SpriteSheetIcons.png",
            "Assets/Art/Font/SpriteAssets/SpriteSheetRenderedIcons.png",
        };

        // Folders literally named "Misc". Phase 1 disposed Graphics/Misc and Graphics/UI/Misc;
        // animation + localization leftovers remain.
        private static readonly string[] UndocumentedMiscFolderGrandfather =
        {
            "Assets/Art/Animations/Misc",
            "Assets/Art/Animations/Probably Not/Misc",
            "Assets/Content/Localization/Table Collections/Misc",
        };

        // First-party asmdefs outside Assets/Scripts/. Taxonomy doc Phase 2 (URPMigration);
        // Assembly-CSharp-Assets.asmdef is Unity's own default root assembly, not ours to move.
        private static readonly string[] AsmdefOutsideScriptsGrandfather =
        {
            "Assets/Assembly-CSharp-Assets.asmdef",
            "Assets/Editor/URPMigration/SS3D.Editor.URPMigration.asmdef",
        };

        public static List<string> GetContentRawArtViolations()
        {
            List<string> violations = new();
            foreach (string path in AllAssetFilePaths())
            {
                if (!path.StartsWith("Assets/Content/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (Array.IndexOf(RawArtExtensions, extension) < 0)
                {
                    continue;
                }

                if (MatchesAnyPrefix(path, ContentRawArtGrandfather))
                {
                    continue;
                }

                violations.Add(path);
            }

            return violations;
        }

        public static List<string> GetScatteredIconViolations()
        {
            List<string> violations = new();
            foreach (string path in AllAssetFilePaths())
            {
                if (path.StartsWith("Assets/Art/Icons/", StringComparison.OrdinalIgnoreCase) || IsVendored(path))
                {
                    continue;
                }

                string extension = Path.GetExtension(path).ToLowerInvariant();
                bool isIconImage = extension == ".svg" ||
                    (extension == ".png" && path.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!isIconImage)
                {
                    continue;
                }

                if (MatchesAnyPrefix(path, ScatteredIconGrandfather))
                {
                    continue;
                }

                violations.Add(path);
            }

            return violations;
        }

        public static List<string> GetUndocumentedMiscFolders()
        {
            List<string> violations = new();
            foreach (string dir in Directory.EnumerateDirectories(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (!string.Equals(Path.GetFileName(dir), "Misc", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relative = ToProjectRelativePath(dir);
                if (IsVendored(relative))
                {
                    continue;
                }

                if (Array.Exists(UndocumentedMiscFolderGrandfather, g => string.Equals(g, relative, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                violations.Add(relative);
            }

            return violations;
        }

        public static List<string> GetAsmdefsOutsideScripts()
        {
            List<string> violations = new();
            foreach (string path in AllAssetFilePaths())
            {
                if (!path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (path.StartsWith("Assets/Scripts/", StringComparison.OrdinalIgnoreCase) || IsVendored(path))
                {
                    continue;
                }

                if (Array.Exists(AsmdefOutsideScriptsGrandfather, g => string.Equals(g, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                violations.Add(path);
            }

            return violations;
        }

        private static IEnumerable<string> AllAssetFilePaths()
        {
            foreach (string absolutePath in Directory.EnumerateFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (absolutePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return ToProjectRelativePath(absolutePath);
            }
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return absolutePath.Substring(projectRoot.Length + 1).Replace('\\', '/');
        }

        private static bool IsVendored(string relativePath)
        {
            return MatchesAnyPrefix(relativePath, VendoredPathPrefixes);
        }

        private static bool MatchesAnyPrefix(string relativePath, string[] prefixesOrPaths)
        {
            foreach (string entry in prefixesOrPaths)
            {
                if (entry.EndsWith("/"))
                {
                    if (relativePath.StartsWith(entry, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (string.Equals(relativePath, entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // --- End asset/file organization taxonomy checks ---------------------------------------

        public static GameObject[] AllPrefabs()
        {
            return GetAssets<GameObject>(PrefabSearchTerm, PrefabRootPath);
        }

        public static SceneAsset[] AllScenes()
        {
            return GetAssets<SceneAsset>(SceneSearchTerm, SceneRootPath);
        }

        public static TileObjectSo[] AllTileObjectSo()
        {
            return GetAssets<TileObjectSo>(TileObjectSoSearchTerm);
        }

        public static ItemObjectSo[] AllItemObjectSo()
        {
            return GetAssets<ItemObjectSo>(ItemObjectSoSearchTerm);
        }

        /// <summary>
        /// Generic getter method to load assets from the project hierarchy. Used to simplify calls from test scripts.
        /// </summary>
        /// <typeparam name="T">Generic type that you want returned.</typeparam>
        /// <param name="searchCriteria">Search criteria for the AssetDatabase.FindAssets() method.</param>
        /// <param name="searchPath">Root path to search in. Will default to assets.</param>
        /// <returns>An array of type T containing all instances within the desired search path.</returns>
        public static T[] GetAssets<T>(string searchCriteria, string searchPath = GenericRootPath) where T : UnityEngine.Object
        {
            // Find all the assets in the project hierarchy (i.e. NOT in a scene)
            string[] guids = AssetDatabase.FindAssets(searchCriteria, new[] { searchPath });

            // Create our array of assets
            T[] returnValues = new T[guids.Length];

            // Populate the array
            for (int i = 0; i < guids.Length; i++)
            {
                returnValues[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return returnValues;
        }

        public static bool CheckMonoBehavioursForCorrectLayer(MonoBehaviour[] behaviours, ref StringBuilder sb)
        {
            bool allRelevantMonoBehavioursAreOnTheRightLayer = true;
            foreach (MonoBehaviour mono in behaviours)
            {
                // Missing scripts appear as null entries; PrefabsDoNotHaveMissingScripts covers those.
                if (mono == null)
                {
                    continue;
                }

                Type monoType = mono.GetType();
                RequiredLayerAttribute attribute = Attribute.GetCustomAttribute(monoType, typeof(RequiredLayerAttribute)) as RequiredLayerAttribute;
                if (attribute == null)
                {
                    continue;
                }
                // Once we are here, we have found a MonoBehaviour with a RequiredLayerAttribute.
                // We now need to test the GameObject to see if it is on the layer that is mandated.

                if (mono.gameObject.layer == LayerMask.NameToLayer(attribute.Layer))
                {
                    continue;
                }

                // The test will fail, as the GameObject SHOULD have had been on a specific layer, but WAS NOT.
                // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                // test to be run multiple times (and only identifying a single breach each time).
                allRelevantMonoBehavioursAreOnTheRightLayer = false;
                GameObject gameObject = mono.gameObject;
                sb.Append($"-> {monoType.Name} script requires object '{gameObject.name}' to be on {attribute.Layer} layer, but it was on {LayerMask.LayerToName(gameObject.layer)} layer.\n");
            }
            return allRelevantMonoBehavioursAreOnTheRightLayer;
        }

        public static bool CheckGameObjectForMissingScripts(GameObject gameobject, ref StringBuilder sb)
        {
            bool allScriptsExist = true;
            MonoBehaviour[] monobehaviours = gameobject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour mono in monobehaviours)
            {
                if (mono == null)
                {
                    allScriptsExist = false;
                    sb.Append($"-> Missing script on '{gameobject.name}'.\n");
                    continue;
                }
            }
            return allScriptsExist;
        }

        public static bool CheckInteractionTargetsHaveSelectable(GameObject gameObject, ref StringBuilder sb)
        {
            bool allTargetsArePickable = true;

            foreach (MonoBehaviour behaviour in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is not IInteractionTarget)
                {
                    continue;
                }

                if (behaviour.GetComponent<Selectable>() != null || behaviour.GetComponentInParent<Selectable>() != null)
                {
                    continue;
                }

                allTargetsArePickable = false;
                sb.Append($"-> {behaviour.GetType().Name} on '{behaviour.gameObject.name}' in '{gameObject.name}' is missing Selectable.\n");
            }

            return allTargetsArePickable;
        }
    }
}
