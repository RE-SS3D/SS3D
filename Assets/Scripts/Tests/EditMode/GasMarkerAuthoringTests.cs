using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.Validation;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTests
{
    public class GasMarkerAuthoringTests
    {
        [Test]
        public void Registry_KeepsOneMarkerPerTile_AndSnapsIt()
        {
            var registry = new GasMarkerRegistry();
            Assert.IsTrue(registry.TryPlace(At(GasMarkerKind.Source, 1.2f, 2.1f)));
            Assert.IsTrue(registry.TryPlace(At(GasMarkerKind.RoomAir, 1f, 2f)));

            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGetAt(new Vector3(1f, 0f, 2f), out GasMarkerRecord marker));
            Assert.AreEqual(GasMarkerKind.RoomAir, marker.Kind);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), marker.Position);
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(-1f, 1f)]
        [TestCase(GasMarkerRecord.MaxPressureKpa + 1f, 1f)]
        [TestCase(100f, float.NaN)]
        [TestCase(100f, float.PositiveInfinity)]
        [TestCase(100f, -1f)]
        public void Registry_RefusesAMarkerTheSimulationCannotTake(float pressure, float fraction)
        {
            GasMarkerRecord marker = At(GasMarkerKind.Source, 0f, 0f);
            marker.PressureKpa = pressure;
            marker.GasFractions = new[] { fraction };
            Assert.IsFalse(new GasMarkerRegistry().TryPlace(marker));
        }

        [Test]
        public void Registry_RefusesALineFillOffAPipeLine()
        {
            GasMarkerRecord marker = At(GasMarkerKind.LineFill, 0f, 0f);
            marker.Line = TileLayer.Turf;
            Assert.IsFalse(new GasMarkerRegistry().TryPlace(marker));
        }

        [Test]
        public void SavedChunk_JsonRoundTrip_KeepsTheMarker()
        {
            GasMarkerRecord marker = At(GasMarkerKind.LineFill, 3f, 4f);
            marker.Line = TileLayer.PipeRight;
            marker.PressureKpa = 250f;
            marker.GasFractions = AtmosGasMix.Pure(AtmosConstants.Plasma);

            string json = JsonUtility.ToJson(new SavedGasMarkerChunkPayload { records = new[] { marker } });
            GasMarkerRecord loaded = JsonUtility.FromJson<SavedGasMarkerChunkPayload>(json).records.Single();

            Assert.AreEqual(GasMarkerKind.LineFill, loaded.Kind);
            Assert.AreEqual(TileLayer.PipeRight, loaded.Line);
            Assert.AreEqual(250f, loaded.PressureKpa);
            Assert.AreEqual(marker.GasFractions, loaded.GasFractions);
            Assert.AreEqual(marker.Position, loaded.Position);
        }

        [Test]
        public void PlaceAndClearCommands_AreInvertible()
        {
            var registry = new GasMarkerRegistry();
            var ctx = new MapEditorCommandContext(null, null, null) { GasMarkers = registry };
            GasMarkerRecord first = At(GasMarkerKind.Source, 1f, 1f);
            GasMarkerRecord second = At(GasMarkerKind.RoomAir, 1f, 1f);

            var place = new PlaceGasMarkerCommand(first, false, default);
            place.Apply(ctx);
            var replace = new PlaceGasMarkerCommand(second, true, first);
            replace.Apply(ctx);
            replace.Revert(ctx);
            Assert.IsTrue(registry.TryGetAt(first.Position, out GasMarkerRecord kept));
            Assert.AreEqual(GasMarkerKind.Source, kept.Kind);

            var clear = new ClearGasMarkerCommand(first.Position, true, first);
            clear.Apply(ctx);
            Assert.AreEqual(0, registry.Count);
            clear.Revert(ctx);
            Assert.AreEqual(1, registry.Count);

            place.Revert(ctx);
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void PlaceCommand_NeedsFloor_AndAValidMarker()
        {
            var objects = new List<GameObject>();
            try
            {
                TileMapTestUtilities.MapContext map = TileMapTestUtilities.CreateContext(objects);
                TileMapTestUtilities.PlacePlenum(map, new Vector3(1f, 0f, 1f));
                var ctx = new MapEditorCommandContext(map.Map, null, null) { GasMarkers = new GasMarkerRegistry() };
                var commands = new List<IMapEditorCommand>();

                Assert.IsTrue(TryPlace(At(GasMarkerKind.Source, 1f, 1f)));
                Assert.IsFalse(TryPlace(At(GasMarkerKind.Source, 5f, 5f)), "No floor");
                GasMarkerRecord invalid = At(GasMarkerKind.Source, 1f, 1f);
                invalid.PressureKpa = float.NaN;
                Assert.IsFalse(TryPlace(invalid), "A client can't send a broken marker");

                bool TryPlace(GasMarkerRecord marker) => MapEditorCommandFactory.TryCreatePlaceCommands(
                    new MapEditorCommandDto { Kind = MapEditorCommandKind.PlaceGasMarker, GasMarker = marker }, ctx, commands);
            }
            finally
            {
                foreach (GameObject go in objects)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CommandDto_CarriesTheMarkerItPlaced()
        {
            GasMarkerRecord marker = At(GasMarkerKind.Source, 2f, 3f);
            MapEditorCommandDto dto = new PlaceGasMarkerCommand(marker, false, default).ToDto();
            Assert.AreEqual(MapEditorCommandKind.PlaceGasMarker, dto.Kind);
            Assert.AreEqual(marker.PressureKpa, dto.GasMarker.PressureKpa);
            Assert.IsTrue(MapEditorGasMarkerCatalog.TryDecode(dto.AssetName, out GasMarkerKind kind));
            Assert.AreEqual(GasMarkerKind.Source, kind);
        }

        [Test]
        public void Catalog_OffersEveryKindOnTheLiveAtmosRail()
        {
            var catalog = new MapEditorCatalog();
            catalog.Build(new GenericObjectSo[0], null);
            Assert.IsFalse(MapEditorCatalog.IsUnavailableScriptingSubcategory(MapEditorSubcategory.Atmospherics));
            Assert.AreEqual(3, catalog.Query(MapEditorMode.Scripting, MapEditorSubcategory.Atmospherics, null).Count());
        }

        [Test]
        public void GasMarkers_LoadAfterSpawnPoints()
        {
            Assert.AreEqual("gas-markers", new GasMarkerPersistenceContributor(() => null).ContributorId);
            Assert.Greater(new GasMarkerPersistenceContributor(() => null).LoadOrder,
                new SpawnPointPersistenceContributor(() => null).LoadOrder);
        }

        [Test]
        public void MapCheck_NamesAnUnsealedMarkerAsASoftWarning()
        {
            var report = new MapValidationReport();
            MapValidator.AddUnsealedGasMarker(report, At(GasMarkerKind.Source, 4f, 5f));
            MapValidationIssue issue = report.Issues.Single();
            StringAssert.Contains("Gas source at (4, 5) isn't in a sealed room", issue.Title);
            Assert.IsTrue(issue.IsWarning);
        }

        [Test]
        public void Form_PressureEditReachesTheDraft_AndSurvivesChromeRefresh()
        {
            MapEditorViewModel model = new() { GasMarkerDraft = GasMarkerRecord.Default(GasMarkerKind.Source) };
            MapEditorView view = new(null, null, model, new MapEditorCatalog(), null);
            // Fields only raise ChangeEvent in a live panel.
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            try
            {
                view.Build(window.rootVisualElement);
                OpenAtmosRail(model);
                view.ShowGasMarkerProperties(editingPlaced: false);
                TextField pressure = view.Root.Q<TextField>("gas-marker-pressure");
                pressure.value = "750";
                model.ShowToast("Unrelated notification");
                Assert.AreSame(pressure, view.Root.Q<TextField>("gas-marker-pressure"));
                Assert.AreEqual(750f, model.GasMarkerDraft.PressureKpa);
            }
            finally { view.Destroy(); window.Close(); }
        }

        [Test]
        public void Form_LineFieldOnlyShowsForALineFill_AndApplyOnlyForAPlacedMarker()
        {
            MapEditorViewModel model = new() { GasMarkerDraft = GasMarkerRecord.Default(GasMarkerKind.LineFill) };
            MapEditorView view = new(null, null, model, new MapEditorCatalog(), null);
            // Fields only raise ChangeEvent in a live panel.
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            try
            {
                view.Build(window.rootVisualElement);
                OpenAtmosRail(model);
                GasMarkerRecord? applied = null;
                view.GasMarkerApplyRequested += marker => applied = marker;

                view.ShowGasMarkerProperties(editingPlaced: false);
                Assert.IsNotNull(view.Root.Q<DropdownField>("gas-marker-line"));
                Assert.IsNull(view.Root.Q<Button>("gas-marker-apply"));

                view.ShowGasMarkerProperties(editingPlaced: true);
                DropdownField line = view.Root.Q<DropdownField>("gas-marker-line");
                line.value = "Waste";
                using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
                {
                    Button apply = view.Root.Q<Button>("gas-marker-apply");
                    submit.target = apply;
                    apply.SendEvent(submit);
                }

                Assert.AreEqual(TileLayer.PipeRight, model.GasMarkerDraft.Line);
                Assert.AreEqual(TileLayer.PipeRight, applied?.Line);
            }
            finally { view.Destroy(); window.Close(); }
        }

        [Test]
        public void Form_ClosesWhenTheRailChanges()
        {
            MapEditorViewModel model = new();
            MapEditorView view = new(null, null, model, new MapEditorCatalog(), null);
            try
            {
                view.Build(new VisualElement());
                OpenAtmosRail(model);
                view.ShowGasMarkerProperties(editingPlaced: false);
                model.SetSubcategory(MapEditorSubcategory.Rooms);
                Assert.IsNull(model.OpenPopover);
            }
            finally { view.Destroy(); }
        }

        private static void OpenAtmosRail(MapEditorViewModel model)
        {
            model.SetMode(MapEditorMode.Scripting);
            model.SetSubcategory(MapEditorSubcategory.Atmospherics);
        }

        private static GasMarkerRecord At(GasMarkerKind kind, float x, float z)
        {
            GasMarkerRecord record = GasMarkerRecord.Default(kind);
            record.Position = new Vector3(x, 0f, z);
            return record;
        }
    }
}
