using System.Collections;
using System.Linq;
using System.Reflection;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>What the Map Editor does to the live world that only shows up with real FishNet spawns.</summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.MapEditor)]
    public class MapEditorScenarios : ScenarioFixture
    {
        /// <summary>Well outside the host's HashGrid window around the body in the test room.</summary>
        private static readonly Vector3 OutsideHostAoi = new(200f, 0f, 200f);

        // Guards: a tile placed outside the host's AOI stays visible during authoring, and AOI applies again on close (PlacedTileObject.HandleHostVisibilityUpdated) · Not cheaper because: FishNet hides the tile at the end of a real server spawn, which EditMode tiles never reach
        [UnityTest]
        public IEnumerator TilePlacedOutsideAoi_IsVisibleWhileAuthoring()
        {
            ChangesRoom = true;
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();

            SetEditorActive(editor, true);
            try
            {
                yield return Hold(0.2f);
                var plenum = (TileObjectSo)tiles.GetAsset("Plenum");
                Assert.IsTrue(tiles.CurrentMap.PlaceTileObject(plenum, OutsideHostAoi, Direction.North, true, false, false,
                    out GameObject instance), "The tile is placed");
                yield return Hold(0.2f);

                PlacedTileObject placed = instance.GetComponent<PlacedTileObject>();
                Assert.IsTrue(placed.NetworkObject.IsSpawned, "The tile is a spawned network object");
                Assert.IsFalse(placed.NetworkObject.Observers.Contains(InstanceFinder.ClientManager.Connection),
                    "The tile is outside the host's AOI");
                Assert.IsTrue(AnyRendererEnabled(placed), "A tile placed in the editor can be seen there");

                SetEditorActive(editor, false);
                yield return null;
                Assert.IsFalse(AnyRendererEnabled(placed), "Outside the editor, AOI hides the tile again");
            }
            finally
            {
                if (editor.IsActive)
                    SetEditorActive(editor, false);
            }
        }

        private static bool AnyRendererEnabled(PlacedTileObject placed) =>
            placed.GetComponentsInChildren<MeshRenderer>(true).Any(renderer => renderer.enabled);

        /// <summary>Opening normally goes through an input action; the scenario only needs the state change.</summary>
        private static void SetEditorActive(MapEditorSubSystem editor, bool active)
        {
            MethodInfo setActive = typeof(MapEditorSubSystem).GetMethod("SetActive", BindingFlags.Instance | BindingFlags.NonPublic);
            setActive.Invoke(editor, new object[] { active });
        }
    }
}
