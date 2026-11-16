using EditorTests;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Placed objects keep their authored setup through save, undo, move and Dropper copy
    /// (Documents/architecture/2026-11_object-setup-persistence.md). A stand-in setup component keeps these off FishNet;
    /// the real devices, the spawn order and container contents are covered by the MapEditor scenarios.
    /// </summary>
    public sealed class ObjectSetupPersistenceTests
    {
        private const string ProbeTileName = "SetupProbeTile";

        private readonly List<GameObject> _instantiated = new();
        private readonly Dictionary<string, GenericObjectSo> _assets = new();

        private sealed class SetupProbe : MonoBehaviour, IPersistentSetup
        {
            [Serializable]
            private sealed class Record
            {
                public int value;
            }

            public int Value;

            public string SetupKey => "probe";

            public string CaptureSetup() => Value == 0 ? null : JsonUtility.ToJson(new Record { value = Value });

            public void RestoreSetup(string json)
            {
                Record record = new();
                if (!string.IsNullOrEmpty(json))
                    JsonUtility.FromJsonOverwrite(json, record);

                Value = record.value;
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _instantiated.Count - 1; i >= 0; i--)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();
            _assets.Clear();
        }

        [Test]
        public void AnObjectAtItsDefaults_WritesNoSetup_AndAChangedOneRoundTrips()
        {
            GameObject source = Track(new GameObject("Source"));
            SetupProbe probe = source.AddComponent<SetupProbe>();
            Assert.IsEmpty(ObjectSetup.Capture(source), "an object nobody configured follows the prefab default");

            probe.Value = 4;
            var record = new SavedPlacedTileObject { tileObjectSOName = ProbeTileName, setup = ObjectSetup.Capture(source) };
            SavedPlacedTileObject loaded = JsonUtility.FromJson<SavedPlacedTileObject>(JsonUtility.ToJson(record));
            Assert.AreEqual(1, loaded.setup.Length);
            Assert.AreEqual("probe", loaded.setup[0].key);

            SetupProbe restored = Track(new GameObject("Restored")).AddComponent<SetupProbe>();
            Assert.IsTrue(ObjectSetup.Restore(restored.gameObject, loaded.setup));
            Assert.AreEqual(4, restored.Value);
        }

        [Test]
        public void ASavedMap_KeepsOnlyChangedSetup_AndPlacingWithItRestoresIt()
        {
            TileMapTestUtilities.MapContext saved = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(3, 0, 3);
            TileMapTestUtilities.PlacePlenum(saved, pos);
            TileObjectSo probeTile = RegisterProbeTile();
            ProbeAt(PlaceProbe(saved, probeTile, pos)).Value = 3;

            string json = JsonUtility.ToJson(saved.Map.Save());
            SavedPlacedTileObject[] records = JsonUtility.FromJson<SavedTileMap>(json).savedChunkList
                .SelectMany(chunk => chunk.savedTiles)
                .SelectMany(tile => tile.GetPlacedObjects())
                .ToArray();
            SavedPlacedTileObject probeRecord = records.Single(r => r.tileObjectSOName == ProbeTileName);
            Assert.AreEqual(1, probeRecord.setup.Length, "the configured object saves its setup");
            Assert.IsEmpty(records.Single(r => r.tileObjectSOName == "TestPlenum").setup, "the plenum is at its defaults");

            TileMapTestUtilities.MapContext loaded = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(loaded, pos);
            Assert.IsTrue(loaded.Map.PlaceTileObject(probeTile, pos, probeRecord.dir, true, false, false,
                probeRecord.setup, null, out GameObject placed));
            Assert.AreEqual(3, ProbeAt(placed).Value);
        }

        [Test]
        public void ASaveFromBeforeSchema3_LoadsAtDefaults()
        {
            const string schema2Record = "{\"id\":\"\",\"tileObjectSOName\":\"" + ProbeTileName + "\",\"origin\":{\"x\":3,\"y\":3},\"dir\":0}";
            SavedPlacedTileObject record = JsonUtility.FromJson<SavedPlacedTileObject>(schema2Record);

            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(3, 0, 3);
            TileMapTestUtilities.PlacePlenum(ctx, pos);
            Assert.IsTrue(ctx.Map.PlaceTileObject(RegisterProbeTile(), pos, record.dir, true, false, false,
                record.setup, null, out GameObject placed));
            Assert.AreEqual(0, ProbeAt(placed).Value);
        }

        [Test]
        public void SetupThatCantBeRestored_IsReportedOncePerObject_AndKeepsTheDefault()
        {
            SetupProbe probe = Track(new GameObject("Probe")).AddComponent<SetupProbe>();
            List<string> skipped = new();

            Assert.IsFalse(ObjectSetup.Restore(probe.gameObject,
                new[] { new SavedSetupEntry("removed-setting", "{}"), new SavedSetupEntry("probe", "{not json") },
                "AtmosValveManual", skipped.Add));

            CollectionAssert.AreEqual(new[] { "AtmosValveManual settings" }, skipped);
            Assert.AreEqual(0, probe.Value);
        }

        [Test]
        public void UndoingADelete_BringsTheSetupBack()
        {
            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(4, 0, 4);
            TileMapTestUtilities.PlacePlenum(ctx, pos);
            TileObjectSo probeTile = RegisterProbeTile();
            MapEditorCommandService service = CreateService(ctx);
            Assert.IsTrue(service.Execute(new PlaceTileCommand(ProbeTileName, pos, Direction.North, false)));
            ProbeAt(Occupant(ctx, probeTile, pos)).Value = 6;

            Assert.IsTrue(service.Execute(new ClearTileCommand(ProbeTileName, pos, Direction.North)));
            Assert.IsNull(Occupant(ctx, probeTile, pos));

            Assert.IsTrue(service.Undo());
            Assert.AreEqual(6, ProbeAt(Occupant(ctx, probeTile, pos)).Value);
        }

        [Test]
        public void AMove_CarriesTheSetup_AndUndoingItBringsItBack()
        {
            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 from = new(5, 0, 5);
            Vector3 to = new(6, 0, 5);
            TileMapTestUtilities.PlacePlenum(ctx, from);
            TileMapTestUtilities.PlacePlenum(ctx, to);
            TileObjectSo probeTile = RegisterProbeTile();
            MapEditorCommandService service = CreateService(ctx);
            Assert.IsTrue(service.Execute(new PlaceTileCommand(ProbeTileName, from, Direction.North, false)));
            ProbeAt(Occupant(ctx, probeTile, from)).Value = 2;

            Assert.IsTrue(service.Execute(new MoveTileCommand(ProbeTileName, from, to, Direction.North)));
            Assert.AreEqual(2, ProbeAt(Occupant(ctx, probeTile, to)).Value);

            Assert.IsTrue(service.Undo());
            Assert.AreEqual(2, ProbeAt(Occupant(ctx, probeTile, from)).Value);
        }

        [Test]
        public void ADropperCopy_CarriesItsSetup_AndAPalettePlaceDoesNot()
        {
            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 copied = new(7, 0, 7);
            Vector3 fromPalette = new(8, 0, 7);
            TileMapTestUtilities.PlacePlenum(ctx, copied);
            TileMapTestUtilities.PlacePlenum(ctx, fromPalette);
            TileObjectSo probeTile = RegisterProbeTile();
            MapEditorCommandService service = CreateService(ctx);
            SavedSetupEntry[] setup = { new("probe", "{\"value\":8}") };

            Assert.IsTrue(service.Execute(MapEditorCommandFactory.FromDto(PlaceDto(copied, setup), service.Context)));
            Assert.IsTrue(service.Execute(MapEditorCommandFactory.FromDto(PlaceDto(fromPalette, null), service.Context)));

            Assert.AreEqual(8, ProbeAt(Occupant(ctx, probeTile, copied)).Value);
            Assert.AreEqual(0, ProbeAt(Occupant(ctx, probeTile, fromPalette)).Value);
        }

        [Test]
        public void AnEditorUsersInWorldChange_IsTheirUndoStep_AndMarksTheMapUnsaved()
        {
            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(9, 0, 9);
            TileMapTestUtilities.PlacePlenum(ctx, pos);
            TileObjectSo probeTile = RegisterProbeTile();
            MapEditorCommandService service = CreateService(ctx);
            SetupProbe probe = ProbeAt(PlaceProbe(ctx, probeTile, pos));
            service.MarkSaved();

            const int editor = 1;
            const int player = 2;
            var tracker = new MapEditorSetupTracker(service);
            tracker.SetEditing(editor, true);

            tracker.Run(player, new[] { probe.gameObject }, () => probe.Value = 1);
            Assert.AreEqual(0, service.UndoDepth, "a player without the editor open is not tracked");
            Assert.IsFalse(service.HasUnsavedChanges);

            tracker.Run(editor, new[] { probe.gameObject }, () => probe.Value = 5);
            Assert.AreEqual(1, service.UndoDepth);
            Assert.IsTrue(service.HasUnsavedChanges, "the Stop prompt and autosave see the change");

            Assert.IsFalse(service.Undo(player), "only its owner undoes it on a shared server");
            Assert.IsTrue(service.Undo(editor));
            Assert.AreEqual(1, probe.Value, "undo puts back the setup from before the change");

            Assert.IsTrue(service.Redo(editor));
            Assert.AreEqual(5, probe.Value);
        }

        [Test]
        public void AnInWorldActionThatChangesNoSetup_IsNoUndoStep()
        {
            TileMapTestUtilities.MapContext ctx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(10, 0, 9);
            TileMapTestUtilities.PlacePlenum(ctx, pos);
            MapEditorCommandService service = CreateService(ctx);
            SetupProbe probe = ProbeAt(PlaceProbe(ctx, RegisterProbeTile(), pos));
            var tracker = new MapEditorSetupTracker(service);
            tracker.SetEditing(1, true);

            tracker.Run(1, new[] { probe.gameObject }, () => { });
            Assert.AreEqual(0, service.UndoDepth);
        }

        [Test]
        public void ANewerSchema_IsFlagged_AndOlderOnesAreNot()
        {
            Assert.IsTrue(PersistenceEnvelope.IsNewerThanThisBuild(PersistenceEnvelope.CurrentSchemaVersion + 1));
            Assert.IsFalse(PersistenceEnvelope.IsNewerThanThisBuild(PersistenceEnvelope.CurrentSchemaVersion));
            Assert.IsFalse(PersistenceEnvelope.IsNewerThanThisBuild(2));

            Assert.IsNull(MapEditorSubSystem.DescribeLoad("Outpost", null, savedByNewerVersion: false));
            StringAssert.Contains("newer version", MapEditorSubSystem.DescribeLoad("Outpost", null, savedByNewerVersion: true));
            StringAssert.Contains("newer version",
                MapEditorSubSystem.DescribeLoad("Outpost", "1 × AtmosValveManual settings", savedByNewerVersion: true));
        }

        [Test]
        public void ASandboxCopyHoldingTheDisk_IsRefusedWhole_AndCostsOnePerItem()
        {
            SavedSetupEntry[] box = Contents(("NuclearAuthenticationDisk", null));
            SavedSetupEntry[] bag = Contents(("Medkit", box));
            MapEditorCommandDto crate = new()
            {
                Kind = MapEditorCommandKind.PlaceTile,
                AssetName = "Locker",
                Setup = Contents(("Backpack", bag), ("HealthScanner", null)),
            };

            Assert.IsTrue(MapEditorSandboxRules.IsDenied(crate), "the disk two containers down still refuses the copy");
            Assert.AreEqual(5, MapEditorSandboxRules.OperationCost(crate), "the locker plus four items");

            crate.Setup = Contents(("HealthScanner", null));
            Assert.IsFalse(MapEditorSandboxRules.IsDenied(crate));
        }

        private static SavedSetupEntry[] Contents(params (string Name, SavedSetupEntry[] Setup)[] items)
        {
            var contents = new ContainerContentsSetup.SavedContents
            {
                items = items.Select((item, index) => new ContainerContentsSetup.SavedContainedItem
                {
                    itemName = item.Name,
                    position = new Vector2Int(index, 0),
                    count = 1,
                    setup = item.Setup,
                }).ToArray(),
            };
            return new[] { new SavedSetupEntry("container:Storage", JsonUtility.ToJson(contents)) };
        }

        private static MapEditorCommandDto PlaceDto(Vector3 position, SavedSetupEntry[] setup) => new()
        {
            Kind = MapEditorCommandKind.PlaceTile,
            AssetName = ProbeTileName,
            Position = position,
            Direction = Direction.North,
            Setup = setup,
        };

        private TileObjectSo RegisterProbeTile()
        {
            GameObject prefab = Track(new GameObject("SetupProbePrefab"));
            prefab.AddComponent<PlacedTileObject>();
            prefab.AddComponent<SetupProbe>();
            prefab.SetActive(false);

            TileObjectSo so = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, ProbeTileName, prefab, "edit-mode-setup-probe");
            so.genericType = TileObjectGenericType.Floor;
            _assets[so.NameString] = so;
            return so;
        }

        private static GameObject PlaceProbe(TileMapTestUtilities.MapContext ctx, TileObjectSo tile, Vector3 pos)
        {
            PlaceResult result = ctx.Construction.TryPlaceTile(tile, pos, Direction.North, replaceExisting: false);
            Assert.IsTrue(result.Success, "the probe is placed");
            return result.Instance;
        }

        private static GameObject Occupant(TileMapTestUtilities.MapContext ctx, TileObjectSo tile, Vector3 pos) =>
            ctx.Map.TryGetTileLocation(tile.layer, pos, out ITileLocation location)
            && location.TryGetPlacedObject(out PlacedTileObject placed)
            && placed != null
                ? placed.gameObject
                : null;

        private static SetupProbe ProbeAt(GameObject placed)
        {
            Assert.IsNotNull(placed, "a probe stands there");
            return placed.GetComponent<SetupProbe>();
        }

        private MapEditorCommandService CreateService(TileMapTestUtilities.MapContext ctx)
        {
            var service = new MapEditorCommandService();
            service.Bind(ctx.Map, loader: null, ctx.Construction);
            service.Context.AssetResolver = name => _assets.TryGetValue(name, out GenericObjectSo asset) ? asset : null;
            return service;
        }

        private GameObject Track(GameObject gameObject)
        {
            _instantiated.Add(gameObject);
            return gameObject;
        }
    }
}
