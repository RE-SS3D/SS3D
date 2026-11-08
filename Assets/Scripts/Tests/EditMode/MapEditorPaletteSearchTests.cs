using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.Validation;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Palette layout S2 (finding things): palette-wide search, Map Check's Show in palette and the
    /// remembered last tab (<c>Documents/architecture/systems/tile.md</c>, Map Editor palette).
    /// </summary>
    public sealed class MapEditorPaletteSearchTests
    {
        private MapEditorCatalogSo _catalogSo;

        [TearDown]
        public void TearDown()
        {
            if (_catalogSo != null)
                Object.DestroyImmediate(_catalogSo);
        }

        [Test]
        public void Search_FindsMatchesInEveryTab_InTabOrder()
        {
            MapEditorCatalog catalog = Synthetic(
                Entry("SteelSheet", MapEditorSubcategory.MaterialsAndParts, "steel"),
                Entry("SteelPlating", MapEditorSubcategory.Floors, "steel", "floor"),
                Entry("TileWhite", MapEditorSubcategory.Floors, "floor"));

            IReadOnlyList<MapEditorSearchGroup> groups = catalog.Search("steel", restrictedPalette: false);

            CollectionAssert.AreEqual(new[] { "Upper › Floors", "Items › Materials & Parts" },
                groups.Select(group => group.Heading).ToArray());
            CollectionAssert.AreEqual(new[] { "SteelPlating" }, groups[0].Entries.Select(entry => entry.AssetName).ToArray());
        }

        [Test]
        public void Search_WithNoTerm_FindsNothing()
        {
            MapEditorCatalog catalog = Synthetic(Entry("SteelPlating", MapEditorSubcategory.Floors, "steel"));

            CollectionAssert.IsEmpty(catalog.Search("  ", restrictedPalette: false));
            CollectionAssert.IsEmpty(catalog.Search(null, restrictedPalette: false));
        }

        [Test]
        public void Search_ReachesScripts()
        {
            MapEditorCatalog catalog = Committed();
            string gasTerm = MapEditorCatalog.SearchTermFor(MapEditorGasMarkerCatalog.Encode(GasMarkerKind.RoomAir));

            Assert.That(SubTabsFound(catalog, "Assistant", restricted: false), Has.Member(MapEditorSubcategory.SpawnPoints));
            Assert.That(SubTabsFound(catalog, gasTerm, restricted: false), Has.Member(MapEditorSubcategory.StartingAir));
        }

        [Test]
        public void Search_ForSandboxPlayers_LeavesOutSpawnPoints()
        {
            MapEditorCatalog catalog = Committed();

            Assert.That(SubTabsFound(catalog, "Assistant", restricted: true), Has.No.Member(MapEditorSubcategory.SpawnPoints));
        }

        [Test]
        public void Search_ByDepartment_FindsItemsInSeveralSubTabs()
        {
            MapEditorSubcategory[] found = SubTabsFound(Committed(), "security", restricted: false);

            Assert.GreaterOrEqual(found.Count(id => MapEditorSubTabs.Get(id).IsItem), 2, string.Join(", ", found));
        }

        /// <summary>A pick switches mode and sub-tab in one step (the subsystem calls this before selecting).</summary>
        [Test]
        public void OpenSubTab_SwitchesModeAndSubTab()
        {
            MapEditorViewModel model = new() { Catalog = Committed() };
            model.SetMode(MapEditorMode.Scripting);
            Assert.IsTrue(model.IsLayerCategoryVisible(TileLayerCategory.Scripts));

            model.OpenSubTab(MapEditorSubcategory.Machines);

            Assert.AreEqual(MapEditorMode.Upper, model.CurrentMode);
            Assert.AreEqual(MapEditorSubcategory.Machines, model.CurrentSubcategory);
            Assert.IsFalse(model.IsLayerCategoryVisible(TileLayerCategory.Scripts), "Scripts markers follow the Scripts tab");
        }

        [Test]
        public void SandboxCapabilities_MoveOffAHiddenSubTab()
        {
            MapEditorViewModel model = new() { Catalog = Committed() };
            model.OpenSubTab(MapEditorSubcategory.SpawnPoints);

            model.SetCapabilities(MapEditorCapabilities.For(persistenceAllowed: false, isSandboxPlayer: true));

            Assert.AreEqual(MapEditorSubcategory.Rooms, model.CurrentSubcategory);
        }

        [TestCase("AtmosPipesSupply", "Atmos Pipes Supply")]
        [TestCase("NuclearAuthenticationDisk", "Nuclear Authentication Disk")]
        [TestCase("IDCard", "ID Card")]
        [TestCase("APC", "APC")]
        [TestCase("AtmosFilterL2", "Atmos Filter L2")]
        [TestCase("M4", "M4")]
        public void PaletteLabel_SplitsNamesIntoWords(string assetName, string expected)
        {
            Assert.AreEqual(expected, MapEditorCatalog.PaletteLabel(assetName));
        }

        [Test]
        public void LastTab_RoundTripsEverySubTab()
        {
            foreach (MapEditorSubTab row in MapEditorSubTabs.All)
                Assert.AreEqual(row.Id, MapEditorLastTab.Decode(MapEditorLastTab.Encode(row.Id), null), row.Label);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Upper/Plumbing")]
        [TestCase("Lower/Floors")]
        [TestCase("Upper/100")]
        [TestCase("Machines")]
        public void LastTab_FallsBackToFloors(string stored)
        {
            Assert.AreEqual(MapEditorSubcategory.Floors, MapEditorLastTab.Decode(stored, null));
        }

        [Test]
        public void LastTab_FallsBackWhenTheSubTabIsHidden()
        {
            string stored = MapEditorLastTab.Encode(MapEditorSubcategory.SpawnPoints);

            Assert.AreEqual("Scripting/SpawnPoints", stored);
            Assert.AreEqual(MapEditorSubcategory.Floors, MapEditorLastTab.Decode(stored, row => !row.SandboxRestricted));
        }

        [Test]
        public void MapCheck_GroupedIssues_PointAtTheFirstMissingEntry()
        {
            MapValidationReport report = MapValidator.Validate("Nuclear Emergency", new[] { "Assistant", "Security" },
                new List<SpawnPointRecord>(), hasNuke: true, hasDisk: false);

            Assert.AreEqual(MapEditorSpawnCatalog.EncodeJob("Assistant"), Issue(report, "job-spawns").EntryKey);
            Assert.AreEqual(MapEditorSpawnCatalog.EncodeAntagonist(AntagonistSpawnCategory.NuclearOperatives),
                Issue(report, "nuclear").EntryKey);
        }

        [Test]
        public void MapCheck_RoomWarnings_PointAtTheMissingDevice()
        {
            MapValidationReport report = new();
            MapValidator.AddRoomDeviceChecks(report, new[]
            {
                new RoomDeviceRecord(RoomDeviceKind.Vent, Vector3.zero, 1, "Bar", roomHasApc: false),
            }, paintedRooms: true);

            Assert.AreEqual(MapValidator.AirAlarmEntry, Issue(report, "air-alarm:1").EntryKey);
            Assert.AreEqual(MapValidator.ScrubberEntry, Issue(report, "no-scrubber:1").EntryKey);
            Assert.AreEqual(MapValidator.ApcEntry, Issue(report, "no-apc:1").EntryKey);
        }

        /// <summary>Every key Map Check can send names a palette entry, and searching its term finds that entry.</summary>
        [Test]
        public void MapCheck_EntryKeys_AreFoundBySearch()
        {
            MapEditorCatalog catalog = Committed();
            string[] keys =
            {
                MapValidator.NukeEntry, MapValidator.DiskEntry, MapValidator.AirAlarmEntry,
                MapValidator.ScrubberEntry, MapValidator.VentEntry, MapValidator.ApcEntry,
                MapEditorSpawnCatalog.EncodeJob("Assistant"),
                MapEditorSpawnCatalog.EncodeAntagonist(AntagonistSpawnCategory.NuclearOperatives),
            };

            foreach (string key in keys)
            {
                Assert.IsTrue(catalog.TryGetEntry(key, out _), $"{key} is not in the palette");
                bool found = catalog.Search(MapEditorCatalog.SearchTermFor(key), restrictedPalette: false)
                    .SelectMany(group => group.Entries)
                    .Any(entry => entry.AssetName == key);
                Assert.IsTrue(found, $"Searching \"{MapEditorCatalog.SearchTermFor(key)}\" does not find {key}");
            }
        }

        private static MapValidationIssue Issue(MapValidationReport report, string id) =>
            report.Issues.Single(issue => issue.Id == id);

        private static MapEditorSubcategory[] SubTabsFound(MapEditorCatalog catalog, string term, bool restricted) =>
            catalog.Search(term, restricted).Select(group => group.SubTab.Id).ToArray();

        private static MapEditorCatalog Committed()
        {
            var committed = AssetDatabase.LoadAssetAtPath<MapEditorCatalogSo>(MapEditorCatalogGeneration.CatalogPath);
            MapEditorCatalog catalog = new();
            catalog.Build(System.Array.Empty<GenericObjectSo>(), committed);
            return catalog;
        }

        private MapEditorCatalog Synthetic(params MapEditorCatalogEntry[] entries)
        {
            _catalogSo = ScriptableObject.CreateInstance<MapEditorCatalogSo>();
            _catalogSo.Entries = entries.ToList();
            MapEditorCatalog catalog = new();
            catalog.Build(System.Array.Empty<GenericObjectSo>(), _catalogSo);
            return catalog;
        }

        private static MapEditorCatalogEntry Entry(string name, MapEditorSubcategory subTab, params string[] tags) =>
            new()
            {
                AssetName = name,
                Mode = MapEditorSubTabs.Get(subTab).Mode,
                Subcategory = subTab,
                SearchTags = tags,
            };
    }
}
