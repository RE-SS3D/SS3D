#if UNITY_EDITOR
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Construction;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Wires Phase 1 melee extensions onto hand and tool prefabs (PrefabUtility — not Human.prefab).
    /// </summary>
    public static class MeleePrefabSetup
    {
        private const string HandLeft = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHandLeft.prefab";
        private const string HandRight = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHandRight.prefab";
        private const string Crowbar = "Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Crowbar.prefab";
        private const string Hatchet = "Assets/Content/WorldObjects/Items/Functional/Tools/Botany/Hatchet.prefab";
        private const string KitchenKnife = "Assets/Content/WorldObjects/Items/Functional/Tools/Kitchen/KitchenKnife.prefab";
        public const string StunBaton = "Assets/Content/WorldObjects/Items/Weapons/StunBaton.prefab";
        private const string StunBatonArt = "Assets/Art/Models/Items/Weapons/Melee/Blunt/";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Combat.Editor.MeleePrefabSetup.SetupBatch</c>
        /// Prefer <see cref="CombatContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[MeleePrefabSetup] Updated {updated} prefabs.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int SetupAll()
        {
            int updated = 0;
            if (EnsureHandMelee(HandLeft))
            {
                updated++;
            }

            if (EnsureHandMelee(HandRight))
            {
                updated++;
            }

            if (EnsureWeapon(Crowbar, MeleeWeaponProfile.Crowbar))
            {
                updated++;
            }

            if (EnsureWeapon(Hatchet, MeleeWeaponProfile.Hatchet))
            {
                updated++;
            }

            if (EnsureWeapon(KitchenKnife, MeleeWeaponProfile.KitchenKnife))
            {
                updated++;
            }

            BuildStunBaton();
            if (EnsureWeapon(StunBaton, MeleeWeaponProfile.StunBaton))
            {
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        /// <summary>
        /// The stun baton's prefab from Tools/export_item_meshes.py output, copied from the crowbar for its melee
        /// wiring; the profile is applied by <see cref="EnsureWeapon"/> afterwards.
        /// </summary>
        private static void BuildStunBaton()
        {
            Mesh mesh = ImportedItemPrefab.BakeMesh(StunBatonArt + "StunBaton.fbx", "StunBaton", StunBatonArt + "StunBatonMesh.asset",
                false, out _, out Material[] sourceMaterials);
            SS3D.Data.AssetDatabases.ObjectAssetReference reference = ImportedItemPrefab.EnsurePrefab(Crowbar, StunBaton, "StunBaton");
            GameObject root = PrefabUtility.LoadPrefabContents(StunBaton);
            try
            {
                root.name = "StunBaton";
                // A baton is no prying tool.
                if (root.TryGetComponent(out ConstructionTool tool)) Object.DestroyImmediate(tool);
                Quaternion gripRotation = DB.LoadAssetAtPath<GameObject>(Crowbar).GetComponent<SS3D.Systems.Inventory.Items.Item>()
                    .AttachmentPoint.localRotation;
                ImportedItemPrefab.ReplaceVisual(root, mesh, ImportedItemPrefab.PaletteMaterials(sourceMaterials), "Attachment");
                Transform grip = ImportedItemPrefab.EnsureChild(root, "Attachment");
                // The handle is the low end of the upright source mesh.
                grip.localPosition = new Vector3(0f, mesh.bounds.min.y + mesh.bounds.size.y * 0.12f, 0f);
                grip.localRotation = gripRotation;
                ImportedItemPrefab.SetItem(root, reference, "Stun Baton", 1f, grip, new Vector3(0f, 0f, 90f), false, Vector3.zero);
                ImportedItemPrefab.WireExamine(root, "items.weapons.stunbaton",
                    "Assets/Content/Data/Examine/String/Items/Weapons/StunBaton.asset", "Stun Baton",
                    "A Security baton with a charged head. A hit knocks the target down and tires them out; it goes through armor.");
                PrefabUtility.SaveAsPrefabAsset(root, StunBaton);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            ImportedItemPrefab.Register(reference, StunBaton, "StunBaton", "Assets/Content/Data/TileMap/Resources/Items/Weapons");
        }

        private static bool EnsureHandMelee(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[MeleePrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out HandMeleeExtension _))
                {
                    root.AddComponent<HandMeleeExtension>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[MeleePrefabSetup] Hand melee on {path}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsureWeapon(string path, MeleeWeaponProfile profile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[MeleePrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out MeleeWeaponItemExtension extension))
                {
                    extension = root.AddComponent<MeleeWeaponItemExtension>();
                }

                SerializedObject so = new(extension);
                so.FindProperty("_profile.BruteDamage").floatValue = profile.BruteDamage;
                so.FindProperty("_profile.BurnDamage").floatValue = profile.BurnDamage;
                so.FindProperty("_profile.WindupSeconds").floatValue = profile.WindupSeconds;
                so.FindProperty("_profile.RecoverySeconds").floatValue = profile.RecoverySeconds;
                so.FindProperty("_profile.StaminaCost").floatValue = profile.StaminaCost;
                so.FindProperty("_profile.CanSever").boolValue = profile.CanSever;
                so.FindProperty("_profile.StaminaDamage").floatValue = profile.StaminaDamage;
                so.FindProperty("_profile.KnockdownSeconds").floatValue = profile.KnockdownSeconds;
                SerializedProperty structuralForce = so.FindProperty("_profile.StructuralForce");
                if (structuralForce != null)
                {
                    structuralForce.floatValue = profile.StructuralForce;
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[MeleePrefabSetup] Weapon melee on {path}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
