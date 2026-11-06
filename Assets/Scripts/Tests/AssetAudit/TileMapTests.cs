using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetAudit
{
    public class TileMapTests
    {
        #region Tests
        [Test, TestCaseSource(nameof(AllTileObjectSo))]
        public void EveryTileObjectSoHasAName(TileObjectSo tileObjectSo)
        {
            Assert.IsTrue(tileObjectSo.NameString != "", $"TileObjectSo '{tileObjectSo.name}' does not have name set.\n");
        }

        [Test, TestCaseSource(nameof(AllItemObjectSo))]
        public void EveryItemObjectSoHasAName(ItemObjectSo itemObjectSo)
        {
            Assert.IsTrue(itemObjectSo.NameString != "", $"ItemObjectSo '{itemObjectSo.name}' does not have name set.\n");
        }

        /// <summary>
        /// Test if every TileObjectSo has a size between 1 and 5
        /// </summary>
        [Test, TestCaseSource(nameof(AllTileObjectSo))]
        public void EveryTileHasRealisticSize(TileObjectSo tile)
        {
            bool invalidSize = tile.height <= 0 || tile.height > 5 || tile.width <= 0 || tile.width > 5;
            Assert.IsFalse(invalidSize, $"TileObjectSo '{tile.name}' has an unrealistic size of { tile.width},{ tile.height}.\n");
        }

        /// <summary>
        /// A networked tile prefab must carry PlacedTileObject on its root. The runtime AddComponent
        /// fallback is server-only, so remote clients never learn the tile exists.
        /// Fix: SS3D.Systems.Tile.Editor.TilePlacedObjectPrefabSetup.SetupAll.
        /// </summary>
        [Test, TestCaseSource(nameof(AllTileObjectSo))]
        public void NetworkedTilePrefabsCarryPlacedTileObject(TileObjectSo tile)
        {
            GameObject prefab = SS3D.Data.Assets.Get<GameObject>(tile.PrefabAsset);
            if (prefab == null || prefab.GetComponent<FishNet.Object.NetworkObject>() == null)
                Assert.Ignore($"TileObjectSo '{tile.name}' has no networked prefab.");

            Assert.IsNotNull(prefab.GetComponent<PlacedTileObject>(),
                $"Tile prefab '{prefab.name}' ({tile.name}) lacks PlacedTileObject; run TilePlacedObjectPrefabSetup.SetupAll.\n");
        }
        /// <summary>
        /// The map editor library and saved maps only know items with an ItemObjectSo under
        /// Content/Data/TileMap/Resources, so a weapon without one cannot be found or placed.
        /// </summary>
        [Test, TestCaseSource(nameof(AllWeaponPrefabs))]
        public void EveryWeaponHasAnItemObjectSo(GameObject weapon)
        {
            Assert.IsTrue(AllItemObjectSo().Any(item => item.PrefabAsset != null && item.NameString == weapon.name),
                $"Weapon '{weapon.name}' has no ItemObjectSo under Content/Data/TileMap/Resources/Items/Weapons.\n");
        }

        /// <summary>A weapon must fit a backpack, whatever it is.</summary>
        [Test, TestCaseSource(nameof(AllWeaponPrefabs))]
        public void EveryWeaponFitsTheBackpack(GameObject weapon)
        {
            GameObject backpack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Clothing/Backpack.prefab");
            AttachedContainer storage = backpack.GetComponentsInChildren<AttachedContainer>(true).First(container => container.HasUi);

            Assert.LessOrEqual(weapon.GetComponent<Item>().SizeClass, storage.MaxSizeClass,
                $"Weapon '{weapon.name}' is larger than the backpack accepts.\n");
        }
        #endregion

        #region Helper functions

        public static TileObjectSo[] AllTileObjectSo()
        {
            return AssetAuditUtilities.AllTileObjectSo();
        }

        public static GameObject[] AllWeaponPrefabs()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content/WorldObjects/Items/Weapons" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
        }

        public static ItemObjectSo[] AllItemObjectSo()
        {
            return AssetAuditUtilities.AllItemObjectSo();
        }
    }

    #endregion
}
