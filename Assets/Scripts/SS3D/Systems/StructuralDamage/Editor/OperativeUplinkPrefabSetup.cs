#if UNITY_EDITOR
using System;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Tier-B recipe for the operative uplink item (antagonist-content.md §4, §6 later pass), run by
    /// <see cref="StructuralDamageContentPrefabRecipes"/> after the Syndicate bomb beacon it is cut from: same
    /// placeholder model (dark box, antenna, red lamp) until someone models an uplink. Swaps the beacon behaviour
    /// for <c>OperativeUplinkController</c>, registers the item and its examine text. Idempotent. Then run
    /// <c>SS3D → Roles → Apply Job Kits</c>, which puts the uplink in the operative kit.
    /// </summary>
    public static class OperativeUplinkPrefabSetup
    {
        public const string Name = "OperativeUplink";
        public const string PrefabPath = "Assets/Content/WorldObjects/Items/Weapons/OperativeUplink.prefab";

        // The controller lives in SS3D.UI.MachineInterface, which this assembly does not reference.
        private const string ControllerType = "SS3D.UI.MachineInterface.OperativeUplinkController, SS3D.UI.MachineInterface";
        private const string ItemsDatabaseId = "c2644d7b34e956743a0bc485e7ca5e37";

        public static void SetupAll()
        {
            if (DB.LoadAssetAtPath<GameObject>(SyndicateBombPrefabSetup.BeaconPrefab) == null)
            {
                throw new InvalidOperationException("Build the Syndicate bomb beacon first (SyndicateBombPrefabSetup)");
            }

            Type controllerType = Type.GetType(ControllerType);
            if (controllerType == null)
            {
                throw new InvalidOperationException("Missing type " + ControllerType);
            }

            if (DB.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                DB.CopyAsset(SyndicateBombPrefabSetup.BeaconPrefab, PrefabPath);
            }

            ObjectAssetReference reference = ExplosivePrefabSetup.Reference(Name, PrefabPath, "Items", ItemsDatabaseId);
            Tile.ItemObjectSo mapItem = ExplosivePrefabSetup.Ensure<Tile.ItemObjectSo>(
                "Assets/Content/Data/TileMap/Resources/Items/Weapons/" + Name + ".asset");
            mapItem.PrefabAsset = reference;
            EditorUtility.SetDirty(mapItem);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.name = Name;
                if (root.TryGetComponent(out SyndicateBombBeacon beacon))
                {
                    UnityEngine.Object.DestroyImmediate(beacon, true);
                }

                if (root.GetComponent(controllerType) == null)
                {
                    root.AddComponent(controllerType);
                }

                SerializedObject item = new(root.GetComponent<Item>());
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.FindProperty("_name").stringValue = "Syndicate uplink";
                item.FindProperty("_weight").floatValue = 0.3f;
                item.FindProperty("_sizeClass").enumValueIndex = (int)SizeClass.Small;
                item.ApplyModifiedPropertiesWithoutUndo();

                SyndicateBombPrefabSetup.WireExamine(root, Name, "Syndicate uplink",
                    "An encrypted Syndicate telecrystal uplink. Use it in hand to spend its telecrystals: what you buy "
                    + "appears in your free hand, or at your feet if both are full. The balance is this device's own, "
                    + "and anyone holding it can spend it.");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            DB.SaveAssets();
        }
    }
}
#endif
