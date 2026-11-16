using System.Linq;
using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class MapEditorDeleteTargetingTests
    {
        private readonly List<GameObject> _instantiated = new();
        private readonly List<MapEditorDeleteTarget> _targets = new();
        private static readonly MethodInfo SetupMethod = typeof(PlacedTileObject).GetMethod(
            "Setup", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>The Shape table's "Delete clears" column, written out independently of the sub-tab table.</summary>
        private static readonly Dictionary<MapEditorSubcategory, MapEditorDeleteScope> ExpectedScopes = new()
        {
            { MapEditorSubcategory.Floors, MapEditorDeleteScope.FloorTurf },
            { MapEditorSubcategory.WallsAndWindows, MapEditorDeleteScope.WallTurf },
            { MapEditorSubcategory.Doors, MapEditorDeleteScope.DoorTurf },
            { MapEditorSubcategory.WallMounts, MapEditorDeleteScope.WallMountFace },
            { MapEditorSubcategory.Furniture, MapEditorDeleteScope.FurnitureLayers },
            { MapEditorSubcategory.Machines, MapEditorDeleteScope.FurnitureLayers },
            { MapEditorSubcategory.Decals, MapEditorDeleteScope.FloorDecal },
            { MapEditorSubcategory.Subfloor, MapEditorDeleteScope.Plenum },
            { MapEditorSubcategory.Cables, MapEditorDeleteScope.Wire },
            { MapEditorSubcategory.GasPipes, MapEditorDeleteScope.PipeLayers },
            { MapEditorSubcategory.DisposalPipes, MapEditorDeleteScope.Disposal },
            { MapEditorSubcategory.Tools, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.MaterialsAndParts, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.Clothing, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.Medical, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.FoodAndDrink, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.Devices, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.Weapons, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.Misc, MapEditorDeleteScope.ItemUnderCursor },
            { MapEditorSubcategory.SpawnPoints, MapEditorDeleteScope.SpawnPoint },
            { MapEditorSubcategory.Rooms, MapEditorDeleteScope.Area },
            { MapEditorSubcategory.StartingAir, MapEditorDeleteScope.GasMarker },
        };

        private static readonly TileObjectGenericType[] TurfKinds =
            { TileObjectGenericType.Floor, TileObjectGenericType.Wall, TileObjectGenericType.Door };

        private static IEnumerable<MapEditorSubcategory> AllRows => MapEditorSubTabs.All.Select(row => row.Id);

        [TearDown]
        public void TearDown()
        {
            for (int i = _instantiated.Count - 1; i >= 0; i--)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();
            _targets.Clear();
        }

        [Test]
        public void EveryRow_DeclaresTheShapeTableScope()
        {
            CollectionAssert.AreEquivalent(ExpectedScopes.Keys, AllRows.ToArray());
            foreach (MapEditorSubTab row in MapEditorSubTabs.All)
                Assert.AreEqual(ExpectedScopes[row.Id], row.DeleteScope, row.Label);
        }

        /// <summary>
        /// A tile with something on every layer: each row's Delete takes exactly the layers its scope names,
        /// so Cables never takes a pipe and Gas Pipes never takes a cable.
        /// </summary>
        [TestCaseSource(nameof(AllRows))]
        public void Resolve_EveryRow_ClearsOnlyItsOwnLayers(MapEditorSubcategory row)
        {
            foreach (TileObjectGenericType turfKind in TurfKinds)
            {
                ITileLocation[] locations = CreateLocations();
                foreach (TileLayer layer in TileHelper.GetTileLayers())
                {
                    if (layer == TileLayer.Turf)
                        PlaceOn(locations, layer, turfKind, "Turf" + turfKind, Direction.South);
                    else
                        PlaceOn(locations, layer, TileObjectGenericType.None, layer.ToString(), Direction.South);
                }

                MapEditorDeleteTargeting.Resolve(row, Direction.South, locations, _targets);

                CollectionAssert.AreEquivalent(
                    ExpectedNames(ExpectedScopes[row], turfKind),
                    _targets.Select(target => target.AssetName).ToArray(),
                    $"{row} over a {turfKind} turf");
            }
        }

        [Test]
        public void Resolve_WallMounts_OnlyMatchingFace()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.WallMountLow, TileObjectGenericType.None, "MountS", Direction.South);
            PlaceOn(locations, TileLayer.WallMountLow, TileObjectGenericType.None, "MountN", Direction.North);

            MapEditorDeleteTargeting.Resolve(
                MapEditorSubcategory.WallMounts, Direction.South, locations, _targets);

            Assert.AreEqual(1, _targets.Count);
            Assert.AreEqual("MountS", _targets[0].AssetName);
            Assert.AreEqual(Direction.South, _targets[0].Direction);
        }

        [Test]
        public void EmptyHint_NamesTheSubTabByItsPath()
        {
            Assert.AreEqual("Nothing to delete on Lower → Cables",
                MapEditorDeleteTargeting.EmptyHint(MapEditorSubcategory.Cables, Direction.North));
            Assert.AreEqual("Nothing to delete on Upper → Wall Mounts (East)",
                MapEditorDeleteTargeting.EmptyHint(MapEditorSubcategory.WallMounts, Direction.East));
        }

        private static string[] ExpectedNames(MapEditorDeleteScope scope, TileObjectGenericType turfKind) =>
            scope switch
            {
                MapEditorDeleteScope.FloorTurf => TurfIf(turfKind, TileObjectGenericType.Floor),
                MapEditorDeleteScope.WallTurf => TurfIf(turfKind, TileObjectGenericType.Wall),
                MapEditorDeleteScope.DoorTurf => TurfIf(turfKind, TileObjectGenericType.Door),
                MapEditorDeleteScope.WallMountFace => Names(TileLayer.WallMountLow, TileLayer.WallMountHigh),
                MapEditorDeleteScope.FurnitureLayers => Names(TileLayer.FurnitureBase, TileLayer.FurnitureTop),
                MapEditorDeleteScope.Plenum => Names(TileLayer.Plenum),
                MapEditorDeleteScope.Wire => Names(TileLayer.Wire),
                MapEditorDeleteScope.PipeLayers => Names(TileLayer.PipeLeft, TileLayer.PipeRight, TileLayer.PipeMiddle,
                    TileLayer.PipeSurface, TileLayer.PipeManifold),
                MapEditorDeleteScope.Disposal => Names(TileLayer.Disposal),
                // Decals, markers, items and rooms are cleared from their own registries, not tile layers.
                _ => new string[0],
            };

        private static string[] TurfIf(TileObjectGenericType placed, TileObjectGenericType wanted) =>
            placed == wanted ? new[] { "Turf" + placed } : new string[0];

        private static string[] Names(params TileLayer[] layers) => layers.Select(layer => layer.ToString()).ToArray();

        private static ITileLocation[] CreateLocations()
        {
            TileLayer[] layers = TileHelper.GetTileLayers();
            var locations = new ITileLocation[layers.Length];
            foreach (TileLayer layer in layers)
                locations[(int)layer] = TileHelper.CreateTileLocation(layer, 0, 0);
            return locations;
        }

        private void PlaceOn(
            ITileLocation[] locations,
            TileLayer layer,
            TileObjectGenericType genericType,
            string name,
            Direction direction)
        {
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(layer, name);
            so.genericType = genericType;

            GameObject go = new(name);
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            Assert.IsNotNull(SetupMethod, "PlacedTileObject.Setup not found");
            SetupMethod.Invoke(placed, new object[] { so, Vector2Int.zero, Vector3.zero, direction, 0 });

            locations[(int)layer].AddPlacedObject(placed, direction);
        }
    }
}
