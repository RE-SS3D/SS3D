using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class MapEditorCatalogMappingTests
    {
        [Test]
        public void Infer_WallObject_MapsToUpperWalls()
        {
            TileObjectSo wall = ScriptableObject.CreateInstance<TileObjectSo>();
            wall.layer = TileLayer.Turf;
            wall.genericType = TileObjectGenericType.Wall;

            MapEditorCatalogEntry entry = MapEditorCatalogHeuristics.Infer(wall);

            Assert.AreEqual(MapEditorMode.Upper, entry.Mode);
            Assert.AreEqual(MapEditorSubcategory.Walls, entry.Subcategory);
        }

        [Test]
        public void Infer_ItemObject_WithoutHints_MapsToItemsMisc()
        {
            ItemObjectSo item = ScriptableObject.CreateInstance<ItemObjectSo>();

            MapEditorCatalogEntry entry = MapEditorCatalogHeuristics.Infer(item);

            Assert.AreEqual(MapEditorMode.Items, entry.Mode);
            Assert.AreEqual(MapEditorSubcategory.Misc, entry.Subcategory);
        }

        [TestCase("Crowbar", null, MapEditorSubcategory.Tools)]
        [TestCase("SodaCanCola", null, MapEditorSubcategory.FoodDrink)]
        [TestCase("DonkPocket", null, MapEditorSubcategory.FoodDrink)]
        [TestCase("BrutePatch", null, MapEditorSubcategory.Medical)]
        [TestCase("Medkit", null, MapEditorSubcategory.Medical)]
        [TestCase("M4", null, MapEditorSubcategory.Security)]
        [TestCase("C4", null, MapEditorSubcategory.Security)]
        [TestCase("X4", null, MapEditorSubcategory.Security)]
        [TestCase("SyndicateMiniBomb", null, MapEditorSubcategory.Security)]
        [TestCase("Taser", null, MapEditorSubcategory.Security)]
        [TestCase("IonRifle", null, MapEditorSubcategory.Security)]
        [TestCase("LaserRifle", null, MapEditorSubcategory.Security)]
        [TestCase("JumpsuitSecurity", null, MapEditorSubcategory.Security)]
        [TestCase("Backpack", null, MapEditorSubcategory.Misc)]
        [TestCase("HealthScanner", "Assets/Content/Data/TileMap/Resources/Items/Functional/Tools/Medical/HealthScanner.asset", MapEditorSubcategory.Medical)]
        [TestCase("Wrench", "Assets/Content/Data/TileMap/Resources/Items/Functional/Tools/Engineering/Wrench.asset", MapEditorSubcategory.Tools)]
        [TestCase("SteelSheet", "Assets/Content/Data/TileMap/Resources/Items/Functional/Materials/SteelSheet.asset", MapEditorSubcategory.Tools)]
        public void ClassifyItem_MapsExpectedSubcategory(string name, string path, MapEditorSubcategory expected)
        {
            Assert.AreEqual(expected, MapEditorCatalogHeuristics.ClassifyItem(name, path));
        }

        [Test]
        public void Query_FiltersBySearchTerm()
        {
            var catalogSo = ScriptableObject.CreateInstance<MapEditorCatalogSo>();
            catalogSo.Entries = new System.Collections.Generic.List<MapEditorCatalogEntry>
            {
                new()
                {
                    AssetName = "SteelPlating",
                    Mode = MapEditorMode.Upper,
                    Subcategory = MapEditorSubcategory.Flooring,
                    SearchTags = new[] { "plating", "floor" },
                },
            };

            MapEditorCatalog catalog = new();
            catalog.Build(System.Array.Empty<GenericObjectSo>(), catalogSo);

            int matches = 0;
            foreach (MapEditorCatalogEntry _ in catalog.Query(MapEditorMode.Upper, MapEditorSubcategory.Flooring, "plating"))
                matches++;

            Assert.AreEqual(1, matches);
        }

        [Test]
        public void Build_IncludesFloorDecalsUnderOverlays()
        {
            MapEditorCatalog catalog = new();
            catalog.Build(System.Array.Empty<GenericObjectSo>(), null);

            int matches = 0;
            foreach (MapEditorCatalogEntry _ in catalog.Query(MapEditorMode.Upper, MapEditorSubcategory.Overlays, null))
                matches++;

            Assert.Greater(matches, 0);
        }

        [Test]
        public void ScriptingSubcategories_AreMarkedUnavailable()
        {
            Assert.IsTrue(MapEditorCatalog.IsScriptingSubcategory(MapEditorSubcategory.SpawnPlacements));
            Assert.IsTrue(MapEditorCatalog.IsScriptingSubcategory(MapEditorSubcategory.Triggers));
        }
    }
}
