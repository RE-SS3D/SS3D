using System.Collections;
using System.Linq;
using System.Reflection;
using FishNet;
using FishNet.Connection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Persistence;
using SS3D.Interactions;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
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

        // Guards: saved setup applied right after ServerManager.Spawn wins over the starting state OnStartServer set (object-setup-persistence Q12, the spawn-timing risk) · Not cheaper because: OnStartServer only runs inside a real FishNet spawn
        [UnityTest]
        public IEnumerator SavedSetup_WinsOverTheValvesStartingState()
        {
            ChangesRoom = true;
            AtmosValveController plain = PlaceValve(RoomTile());
            bool startsOpen = plain.IsOpen;
            Assert.IsEmpty(ObjectSetup.Capture(plain.gameObject), "a valve at its starting state saves no setup");

            SavedSetupEntry[] flipped = { new("atmos-device", $"{{\"enabled\":{(!startsOpen).ToString().ToLowerInvariant()}}}") };
            var valveSo = (TileObjectSo)Tiles.GetAsset(ManualValve);
            Assert.IsTrue(Tiles.CurrentMap.PlaceTileObject(valveSo, RoomTile() + Vector3.right, Direction.North, true, false, false,
                flipped, null, out GameObject configured));

            AtmosValveController valve = configured.GetComponent<AtmosValveController>();
            Assert.AreEqual(!startsOpen, valve.IsOpen, "the saved setup is applied after OnStartServer, not overwritten by it");
            Assert.AreEqual(1, ObjectSetup.Capture(configured).Length, "and saving it again keeps it");
            yield return null;
        }

        // Guards: an editor user turning a valve by hand records an undo step they own, the map reads unsaved for the Stop prompt, and undo turns it back (MapEditorSetupTracker hooked into CmdRunInteraction, RpcSetEditorOpen) · Not cheaper because: the change crosses FishNet's interaction and editor RPCs, which EditMode can't reach
        [UnityTest]
        public IEnumerator AnEditorUsersValveTurn_IsAnUndoStep_ThatLeavesTheMapUnsaved()
        {
            ChangesRoom = true;
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            AtmosValveController valve = PlaceValve(RoomTile());
            bool startsOpen = valve.IsOpen;
            var host = (NetworkConnection)Host.Owner;

            SetEditorActive(editor, true);
            try
            {
                yield return WaitFor(() => editor.IsEditing(host), "the server knows the editor is open");
                long revision = editor.EditRevision;

                string refusal = Body.GetComponent<InteractionController>().RunInteractionForTesting(
                    valve.gameObject, valve.transform.position, "Toggle", IntentType.Help);
                Assert.IsNull(refusal, refusal);
                yield return WaitFor(() => valve.IsOpen != startsOpen, "the turn goes through");

                Assert.AreNotEqual(revision, editor.EditRevision, "the turn is an edit");
                Assert.IsTrue(editor.HasUnsavedChanges, "Stop prompts to save it");

                typeof(MapEditorSubSystem).GetMethod("OnUndoRequested", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(editor, null);
                yield return WaitFor(() => valve.IsOpen == startsOpen, "undo turns the valve back");
            }
            finally
            {
                if (editor.IsActive)
                    SetEditorActive(editor, false);
            }

            yield return WaitFor(() => !editor.IsEditing(host), "closing the editor stops the tracking");
        }

        // Guards: a locker's contents save recursively and come back whole, three containers deep, and restoring twice leaves one copy (AttachedContainer setup, Q6/Q13) · Not cheaper because: contents are real spawned items found through the station's asset catalog
        [UnityTest]
        public IEnumerator ALockersNestedContents_ComeBackWhole()
        {
            ChangesRoom = true;
            var lockerSo = (TileObjectSo)Tiles.GetAsset("Locker");
            Vector3 position = RoomTile();
            AttachedContainer storage = StorageOf(PlaceTile(lockerSo, position));
            AttachedContainer backpack = Store("Backpack", storage);
            AttachedContainer medkit = Store("Medkit", backpack);
            Store("HealthScanner", medkit);

            // Let the stores reach the host's client before the locker goes (FishNet's ClientHost reads a despawned
            // container's late sync against stale references).
            yield return Hold(0.3f);
            SavedSetupEntry[] setup = ObjectSetup.Capture(storage.GetComponentInParent<PlacedTileObject>().gameObject);
            Tiles.CurrentMap.ClearTileObject(position, lockerSo.layer, Direction.North);
            yield return null;

            Assert.IsTrue(Tiles.CurrentMap.PlaceTileObject(lockerSo, position, Direction.North, true, false, false,
                setup, null, out GameObject restoredLocker));
            ObjectSetup.Restore(restoredLocker, setup);
            yield return Hold(0.3f);

            Item restoredBackpack = OnlyItem(StorageOf(restoredLocker), "Backpack");
            Item restoredMedkit = OnlyItem(restoredBackpack.GetComponentInChildren<AttachedContainer>(), "Medkit");
            OnlyItem(restoredMedkit.GetComponentInChildren<AttachedContainer>(), "HealthScanner");
        }

        private const string ManualValve = "AtmosValveManual";

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        private Vector3 RoomTile()
        {
            Vector3 body = Body.transform.position;
            return new Vector3(Mathf.Round(body.x) + 1f, 0f, Mathf.Round(body.z) + 1f);
        }

        private static AtmosValveController PlaceValve(Vector3 position) =>
            PlaceTile((TileObjectSo)Tiles.GetAsset(ManualValve), position).GetComponent<AtmosValveController>();

        private static GameObject PlaceTile(TileObjectSo tile, Vector3 position)
        {
            Assert.IsTrue(Tiles.CurrentMap.PlaceTileObject(tile, position, Direction.North, true, false, false, out GameObject placed),
                $"{tile.NameString} places at {position}");
            return placed;
        }

        private static AttachedContainer StorageOf(GameObject placed) => placed.GetComponentInChildren<AttachedContainer>();

        private static AttachedContainer Store(string itemName, AttachedContainer into)
        {
            var itemSo = (ItemObjectSo)Tiles.GetAsset(itemName);
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(Assets.Get<GameObject>(itemSo.PrefabAsset), into);
            Assert.IsNotNull(item, $"{itemName} fits in {into.name}");
            return item.GetComponentInChildren<AttachedContainer>();
        }

        private static Item OnlyItem(AttachedContainer container, string expected)
        {
            Item[] items = container.Items.ToArray();
            Assert.AreEqual(1, items.Length, $"{container.name} holds exactly one item");
            StringAssert.StartsWith(expected, items[0].name);
            return items[0];
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
