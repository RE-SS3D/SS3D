using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// The floor under an airlock: each half tile copies the floor on its side of the door, and a side
    /// with no floor leaves its half open so the door cell's plenum shows (RE-SS3D/SS3D#1299).
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Construction)]
    public class AirlockFloorScenarios : ScenarioFixture
    {
        /// <summary>Open space well away from the test room.</summary>
        private static readonly Vector3 DoorCell = new(40f, 0f, 40f);

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        // Guards: placing and clearing a floor beside a door re-dresses its half tiles, and an open half uncovers the plenum (DoorAdjacencyConnector.RefreshSideFloors via TileMap) · Not cheaper because: the floor ids resolve through the TileSubSystem asset catalog, which EditMode maps never boot
        [UnityTest]
        public IEnumerator AirlockHalfTiles_FollowTheFloorsBesideTheDoor()
        {
            Vector3 north = DoorCell + Vector3.forward;
            Vector3 south = DoorCell + Vector3.back;
            try
            {
                Place("Plenum", north);
                Place("Plenum", DoorCell);
                Place("Plenum", south);
                GameObject door = Place("CivillianAirlock", DoorCell);
                Place("TileWood", north);
                yield return null;

                DoorAdjacencyConnector connector = door.GetComponent<DoorAdjacencyConnector>();
                AirlockFloorVisual visual = door.GetComponentInChildren<AirlockFloorVisual>(true);
                Assert.IsNotNull(visual, "The door has its floor visual");
                Assert.AreSame(FloorMaterial("TileWood"), ActiveHalfMaterial(visual, 0), "The north half wears the wood floor north of the door");
                Assert.IsNull(ActiveHalfMaterial(visual, 1), "The south half is open: only bare plenum lies south");
                Assert.IsFalse(connector.CoversPlenum, "An open half uncovers the door cell");
                Assert.IsFalse(TileUnderfloorVisibility.ShouldHideUnderfloor(Tiles.CurrentMap, DoorCell, TileLayer.Plenum, false),
                    "The plenum under the door shows through the open half");

                Place("TileGrey", south);
                yield return null;
                Assert.AreSame(FloorMaterial("TileGrey"), ActiveHalfMaterial(visual, 1), "Tiling the south side dresses the south half");
                Assert.IsTrue(connector.CoversPlenum, "With both halves floored, the door covers its plenum again");
                Assert.IsTrue(TileUnderfloorVisibility.ShouldHideUnderfloor(Tiles.CurrentMap, DoorCell, TileLayer.Wire, false),
                    "Wiring under a fully floored door is hidden");

                Tiles.CurrentMap.ClearTileObject(north, TileLayer.Turf, Direction.North);
                yield return null;
                Assert.IsNull(ActiveHalfMaterial(visual, 0), "Prying up the north floor opens the north half");
                Assert.IsFalse(connector.CoversPlenum);
            }
            finally
            {
                // Clearing a plenum takes the turf above it along.
                Tiles.CurrentMap.ClearTileObject(north, TileLayer.Plenum, Direction.North);
                Tiles.CurrentMap.ClearTileObject(DoorCell, TileLayer.Plenum, Direction.North);
                Tiles.CurrentMap.ClearTileObject(south, TileLayer.Plenum, Direction.North);
            }
        }

        private static GameObject Place(string assetName, Vector3 position)
        {
            var tile = (TileObjectSo)Tiles.GetAsset(assetName);
            Assert.IsTrue(Tiles.CurrentMap.PlaceTileObject(tile, position, Direction.North, true, false, false, out GameObject placed),
                $"{assetName} places at {position}");
            return placed;
        }

        private static Material FloorMaterial(string assetName)
        {
            var tile = (TileObjectSo)Tiles.GetAsset(assetName);
            return Assets.Get<GameObject>(tile.PrefabAsset).GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
        }

        private static Material ActiveHalfMaterial(AirlockFloorVisual visual, int submesh)
        {
            MeshRenderer half = visual.GetHalf(submesh);
            return half != null && half.gameObject.activeSelf ? half.sharedMaterial : null;
        }
    }
}
