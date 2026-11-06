using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Tests.EditMode
{
    public sealed class TileUnderfloorVisibilityTests
    {
        private readonly List<Object> _owned = new();
        private readonly List<GameObject> _instantiated = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _instantiated.Count; i++)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();

            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                    Object.DestroyImmediate(_owned[i]);
            }

            _owned.Clear();
        }

        [Test]
        public void IsUnderfloorLayer_IncludesPlenumWiresDisposalAndUnderfloorPipes()
        {
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.Plenum));
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.Wire));
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.Disposal));
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.PipeLeft));
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.PipeMiddle));
            Assert.IsTrue(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.PipeRight));
        }

        [Test]
        public void IsUnderfloorLayer_ExcludesPipeSurfaceAndTurf()
        {
            Assert.IsFalse(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.PipeSurface));
            Assert.IsFalse(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.Turf));
            Assert.IsFalse(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.FurnitureBase));
            Assert.IsFalse(TileUnderfloorVisibility.IsUnderfloorLayer(TileLayer.WallMountHigh));
        }

        [Test]
        public void IsCoveringTurfGenericType_FloorWallDoorOnly()
        {
            Assert.IsTrue(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.Floor));
            Assert.IsTrue(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.Wall));
            Assert.IsTrue(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.Door));

            Assert.IsFalse(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.Plenum));
            Assert.IsFalse(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.Pipe));
            Assert.IsFalse(TileUnderfloorVisibility.IsCoveringTurfGenericType(TileObjectGenericType.None));
        }

        [Test]
        public void CellHasCoveringTurf_False_WhenOnlyPlenum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(2f, 0f, 2f);
            TileMapTestUtilities.PlacePlenum(context, position);

            Assert.IsFalse(TileUnderfloorVisibility.CellHasCoveringTurf(context.Map, position));
            Assert.IsFalse(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, position, TileLayer.Wire, mapEditorAuthoring: false));
        }

        [Test]
        public void CellHasCoveringTurf_True_ForFloorWallAndDoor()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 floorPos = new Vector3(1f, 0f, 1f);
            Vector3 wallPos = new Vector3(2f, 0f, 1f);
            Vector3 doorPos = new Vector3(3f, 0f, 1f);

            PlaceTurf(context, floorPos, TileObjectGenericType.Floor, "TestFloor");
            PlaceTurf(context, wallPos, TileObjectGenericType.Wall, "TestWall");
            PlaceTurf(context, doorPos, TileObjectGenericType.Door, "TestDoor");

            Assert.IsTrue(TileUnderfloorVisibility.CellHasCoveringTurf(context.Map, floorPos));
            Assert.IsTrue(TileUnderfloorVisibility.CellHasCoveringTurf(context.Map, wallPos));
            Assert.IsTrue(TileUnderfloorVisibility.CellHasCoveringTurf(context.Map, doorPos));
        }

        [Test]
        public void ShouldHideUnderfloor_False_WhenMapEditorAuthoring()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(4f, 0f, 4f);
            PlaceTurf(context, position, TileObjectGenericType.Floor, "AuthoringFloor");

            Assert.IsTrue(TileUnderfloorVisibility.CellHasCoveringTurf(context.Map, position));
            Assert.IsFalse(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, position, TileLayer.Wire, mapEditorAuthoring: true));
        }

        [Test]
        public void ShouldHideUnderfloor_True_WhenPlayModeAndFloorPresent()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(5f, 0f, 5f);
            PlaceTurf(context, position, TileObjectGenericType.Floor, "PlayFloor");

            Assert.IsTrue(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, position, TileLayer.Wire, mapEditorAuthoring: false));
        }

        [Test]
        public void ShouldHideUnderfloor_Plenum_RequiresAllEightSurroundingCellsCovered()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 centre = new Vector3(5f, 0f, 5f);
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 1 && z == 1)
                        continue;
                    PlaceTurf(context, centre + new Vector3(x, 0f, z), TileObjectGenericType.Floor, "Floor");
                }

            Assert.IsFalse(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, centre, TileLayer.Plenum, mapEditorAuthoring: false),
                "A diagonal gap exposes the plenum");
            Assert.IsTrue(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, centre, TileLayer.Wire, mapEditorAuthoring: false),
                "Other underfloor layers follow their own cell");

            PlaceTurf(context, centre + new Vector3(1f, 0f, 1f), TileObjectGenericType.Wall, "Wall");

            Assert.IsTrue(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, centre, TileLayer.Plenum, mapEditorAuthoring: false));
        }

        [Test]
        public void ShouldHideUnderfloor_Plenum_FalseOnBareCellItself()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 centre = new Vector3(5f, 0f, 5f);
            Assert.IsFalse(TileUnderfloorVisibility.ShouldHideUnderfloor(context.Map, centre, TileLayer.Plenum, mapEditorAuthoring: false));
        }

        private void PlaceTurf(
            TileMapTestUtilities.MapContext context,
            Vector3 position,
            TileObjectGenericType genericType,
            string name)
        {
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, name);
            turfSo.genericType = genericType;
            _owned.Add(turfSo);
            _owned.Add(turfSo.PrefabAsset);

            bool placed = context.Map.PlaceTileObject(
                turfSo,
                position,
                Direction.North,
                skipBuildCheck: true,
                replaceExisting: false,
                skipAdjacency: true,
                out GameObject go);
            Assert.IsTrue(placed, $"Expected {genericType} turf placement to succeed.");
            if (go != null)
                _instantiated.Add(go);
        }
    }
}
