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
        public void Resolve_Flooring_ClearsFloorTurf()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.Turf, TileObjectGenericType.Floor, "Floor", Direction.North);

            MapEditorDeleteTargeting.Resolve(MapEditorSubcategory.Flooring, Direction.North, locations, _targets);

            Assert.AreEqual(1, _targets.Count);
            Assert.AreEqual("Floor", _targets[0].AssetName);
        }

        [Test]
        public void Resolve_Walls_IgnoresFloorTurf()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.Turf, TileObjectGenericType.Floor, "Floor", Direction.North);

            MapEditorDeleteTargeting.Resolve(MapEditorSubcategory.Walls, Direction.North, locations, _targets);

            Assert.IsEmpty(_targets);
        }

        [Test]
        public void Resolve_WallAttachments_OnlyMatchingFace()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.WallMountLow, TileObjectGenericType.None, "MountS", Direction.South);
            PlaceOn(locations, TileLayer.WallMountLow, TileObjectGenericType.None, "MountN", Direction.North);

            MapEditorDeleteTargeting.Resolve(
                MapEditorSubcategory.WallAttachments, Direction.South, locations, _targets);

            Assert.AreEqual(1, _targets.Count);
            Assert.AreEqual("MountS", _targets[0].AssetName);
            Assert.AreEqual(Direction.South, _targets[0].Direction);
        }

        [Test]
        public void Resolve_Piping_TakesPipesBeforeTheWireOnTheirTile_NotDisposal()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.Wire, TileObjectGenericType.None, "Cable", Direction.North);
            PlaceOn(locations, TileLayer.PipeMiddle, TileObjectGenericType.Pipe, "Pipe", Direction.North);
            PlaceOn(locations, TileLayer.Disposal, TileObjectGenericType.Disposal, "Disp", Direction.North);

            MapEditorDeleteTargeting.Resolve(MapEditorSubcategory.Piping, Direction.North, locations, _targets);

            Assert.AreEqual("Pipe", _targets.Single().AssetName);
        }

        [Test]
        public void Resolve_Piping_TakesTheWireOnceNoPipeIsLeft()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.Wire, TileObjectGenericType.None, "Cable", Direction.North);

            MapEditorDeleteTargeting.Resolve(MapEditorSubcategory.Piping, Direction.North, locations, _targets);

            Assert.AreEqual("Cable", _targets.Single().AssetName);
        }

        [Test]
        public void Resolve_Uncategorized_IsEmpty()
        {
            ITileLocation[] locations = CreateLocations();
            PlaceOn(locations, TileLayer.Turf, TileObjectGenericType.Floor, "Floor", Direction.North);

            MapEditorDeleteTargeting.Resolve(
                MapEditorSubcategory.Uncategorized, Direction.North, locations, _targets);

            Assert.IsEmpty(_targets);
        }

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
