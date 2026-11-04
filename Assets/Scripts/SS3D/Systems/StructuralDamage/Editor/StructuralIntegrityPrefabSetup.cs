#if UNITY_EDITOR
using SS3D.Systems.Examine;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Adds <see cref="StructuralIntegrityPresenter"/> and swaps SimpleExaminable →
    /// StructuralIntegrityExaminable on wall/window prefabs that bake PlacedTileObject.
    /// </summary>
    public static class StructuralIntegrityPrefabSetup
    {
        private const string WindLightClip = "Assets/Art/Sound/World/Ambience/Air/WindLight.wav";

        private static readonly string[] PrefabPaths =
        {
            "Assets/Content/WorldObjects/Structures/Walls/SteelWall.prefab",
            "Assets/Content/WorldObjects/Structures/Walls/SteelWallReinforced.prefab",
            "Assets/Content/WorldObjects/Structures/Walls/SteelWindow.prefab",
            "Assets/Content/WorldObjects/Structures/Walls/SteelWindowReinforced.prefab",
            "Assets/Content/WorldObjects/Structures/Walls/SteelGirder.prefab",
            "Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableSteel.prefab",
            "Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableWood.prefab",
            "Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableGlass.prefab",
            "Assets/Content/WorldObjects/Structures/Doors/CivillianAirlock.prefab",
            "Assets/Content/WorldObjects/Structures/Doors/SecurityAirlock.prefab",
        };

        private static readonly string[] MetalFurnitureDefinitions =
        {
            "Storage/Lockers/Locker", "Storage/Lockers/SecureLocker", "Storage/Lockers/SecurityLocker",
            "Machines/Vendors/VendorYouTool", "Machines/Vendors/VendorSecTech",
            "Machines/Vendors/VendorRobustSoftdrinks", "Machines/Vendors/VendorHotDrinks",
            "Machines/Engineering/SMES", "Machines/Engineering/PacManGenerator",
            "Machines/Engineering/PowerCellCharger", "Machines/Medical/ChemicalDispenser",
            "Machines/Kitchen/Microwave", "Machines/Kitchen/Blender", "Machines/Kitchen/Gibber",
            "Machines/Kitchen/TableBoozeDispenser", "Machines/Generic/SlotMachine",
        };

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.StructuralIntegrityPrefabSetup.SetupBatch</c>
        /// Prefer <see cref="StructuralDamageContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[StructuralIntegrityPrefabSetup] Updated {updated} prefabs.");
            if (Application.isBatchMode)
                EditorApplication.Exit(updated > 0 ? 0 : 1);
        }

        public static int SetupAll()
        {
            AudioClip hiss = AssetDatabase.LoadAssetAtPath<AudioClip>(WindLightClip);
            int updated = 0;
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                if (SetupPrefab(PrefabPaths[i], hiss))
                    updated++;
            }

            foreach (string definition in MetalFurnitureDefinitions)
            {
                string assetPath = $"Assets/Content/Data/TileMap/Resources/Furniture/{definition}.asset";
                TileObjectSo so = AssetDatabase.LoadAssetAtPath<TileObjectSo>(assetPath);
                if (so == null)
                {
                    Debug.LogError($"[StructuralIntegrityPrefabSetup] Missing {assetPath}");
                    continue;
                }
                if (!so.structuralDamageEnabled)
                {
                    so.structuralDamageEnabled = true;
                    EditorUtility.SetDirty(so);
                }
                string prefabDefinition = definition == "Storage/Lockers/SecureLocker"
                    ? "Storage/Lockers/LockerSecure" : definition;
                if (SetupPrefab($"Assets/Content/WorldObjects/Furniture/{prefabDefinition}.prefab", null))
                    updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool SetupPrefab(string path, AudioClip hiss)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[StructuralIntegrityPrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out PlacedTileObject _))
                {
                    Debug.LogWarning($"[StructuralIntegrityPrefabSetup] Skip (no PlacedTileObject): {path}");
                    return false;
                }

                bool changed = false;

                if (!root.TryGetComponent(out StructuralIntegrityPresenter presenter))
                {
                    presenter = root.AddComponent<StructuralIntegrityPresenter>();
                    changed = true;
                }

                SerializedObject presenterSo = new SerializedObject(presenter);
                SerializedProperty clipProp = presenterSo.FindProperty("_crackedHiss");
                if (path.Contains("/Walls/") && clipProp != null && clipProp.objectReferenceValue == null && hiss != null)
                {
                    clipProp.objectReferenceValue = hiss;
                    presenterSo.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (EnsureStructuralExaminable(root))
                    changed = true;

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsureStructuralExaminable(GameObject root)
        {
            if (root.TryGetComponent(out StructuralIntegrityExaminable _))
                return false;

            ExamineData data = null;
            if (root.TryGetComponent(out SimpleExaminable simple))
            {
                SerializedObject simpleSo = new SerializedObject(simple);
                SerializedProperty keyProp = simpleSo.FindProperty("key");
                if (keyProp != null)
                    data = keyProp.objectReferenceValue as ExamineData;

                Object.DestroyImmediate(simple, true);
            }

            StructuralIntegrityExaminable examinable = root.AddComponent<StructuralIntegrityExaminable>();
            if (data != null)
            {
                SerializedObject examinableSo = new SerializedObject(examinable);
                SerializedProperty keyProp = examinableSo.FindProperty("key");
                if (keyProp != null)
                {
                    keyProp.objectReferenceValue = data;
                    examinableSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return true;
        }
    }
}
#endif
