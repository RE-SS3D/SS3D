using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Construction;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Every floor and wall tile in the game has a construction answer, so a new tile cannot silently
    /// vanish when pried or be impossible to take apart (2026-11_construction-mvp1 D17). A failure names
    /// the tile and the table to add it to.
    /// </summary>
    public class ConstructionCoverageTests
    {
        private static List<TileObjectSo> TilesOf(TileObjectGenericType type) =>
            Resources.LoadAll<TileObjectSo>("")
                .Where(tile => tile != null && tile.PrefabAsset != null && tile.layer == TileLayer.Turf && tile.genericType == type)
                .ToList();

        private static Dictionary<string, GenericObjectSo> Catalog()
        {
            Dictionary<string, GenericObjectSo> byName = new();
            foreach (GenericObjectSo asset in Resources.LoadAll<GenericObjectSo>(""))
            {
                if (asset != null && asset.PrefabAsset != null)
                {
                    byName[asset.NameString] = asset;
                }
            }

            return byName;
        }

        // Guards: a new floor tile either pries up into its own tile item or is refused, never lost (D17)
        [Test]
        public void EveryFloorTile_HasATileItemOrIsListedAsNotPryable()
        {
            HashSet<string> kinds = new(ConstructionFloorTiles.Kinds.Select(k => k.Turf));
            HashSet<string> refused = new(ConstructionFloorTiles.NotPryable.Select(k => k.Turf));
            List<string> floors = TilesOf(TileObjectGenericType.Floor).Select(tile => tile.NameString).Distinct().ToList();
            Assert.IsNotEmpty(floors);

            List<string> uncovered = floors.Where(name => !kinds.Contains(name) && !refused.Contains(name)).ToList();
            Assert.IsEmpty(uncovered,
                "Floor tiles with no construction answer: " + string.Join(", ", uncovered)
                + ". Add each to ConstructionFloorTiles.Kinds (a tile item, built by SS3D → Construction → Run Content"
                + " Prefab Recipes) or to ConstructionFloorTiles.NotPryable with a reason.");

            List<string> both = floors.Where(name => kinds.Contains(name) && refused.Contains(name)).ToList();
            Assert.IsEmpty(both, "Floor tiles both pryable and refused: " + string.Join(", ", both));

            List<string> stale = kinds.Concat(refused).Where(name => !floors.Contains(name)).ToList();
            Assert.IsEmpty(stale, "ConstructionFloorTiles names floors that do not exist: " + string.Join(", ", stale));
        }

        // Guards: each floor tile item exists, is a floor-tile construction material and stacks
        [Test]
        public void EveryFloorTileItem_IsAStackableFloorTileMaterial()
        {
            Dictionary<string, GenericObjectSo> catalog = Catalog();
            foreach ((string turf, string itemName, string displayName, bool _) in ConstructionFloorTiles.Kinds)
            {
                Assert.IsTrue(catalog.TryGetValue(itemName, out GenericObjectSo asset) && asset is ItemObjectSo,
                    $"{turf}: no item asset {itemName}; run SS3D → Construction → Run Content Prefab Recipes");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(asset.PrefabAsset.Id));
                Assert.IsNotNull(prefab, $"{itemName}: prefab missing");
                Assert.IsTrue(prefab.TryGetComponent(out ConstructionMaterial material) && material.Input == ConstructionInput.FloorTile,
                    $"{itemName}: needs a FloorTile ConstructionMaterial");
                Item item = prefab.GetComponent<Item>();
                Assert.Greater(item.MaxStackSize, 1, $"{itemName}: floor tiles stack");
                Assert.AreEqual(displayName, item.Name, $"{itemName}: display name");
            }
        }

        // Guards: a new plain wall tile can be taken apart, or is reinforced, or is listed with a reason (§9)
        [Test]
        public void EveryWallTile_IsDeconstructibleReinforcedOrListed()
        {
            HashSet<string> onLadder = new(ConstructionRecipes.Rules
                .Where(rule => rule.Target == ConstructionTargetKind.NamedTile && rule.RequiresIntact)
                .Select(rule => rule.TargetName));
            HashSet<string> listed = new(ConstructionRecipes.NotDeconstructible.Select(k => k.Tile));

            List<string> uncovered = TilesOf(TileObjectGenericType.Wall)
                .Where(tile => !tile.reinforced && !onLadder.Contains(tile.NameString) && !listed.Contains(tile.NameString))
                .Select(tile => tile.NameString).Distinct().ToList();
            Assert.IsEmpty(uncovered,
                "Wall tiles with no way to take them apart: " + string.Join(", ", uncovered)
                + ". Add a deconstruction rule to ConstructionRecipes, set TileObjectSo.reinforced, or list it in"
                + " ConstructionRecipes.NotDeconstructible with a reason.");
        }
    }
}
