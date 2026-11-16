using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class AreaBoundaryEvaluatorTests
    {
        private List<GameObject> _instantiated;

        [SetUp]
        public void SetUp() => _instantiated = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [Test]
        public void IsWalkable_RequiresPlenumAndNoWall()
        {
            TileMapTestUtilities.MapContext context = CreateMap();
            Vector3 center = new Vector3(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(context, center);

            TileCoord coord = context.Query.WorldToTile(center, context.Map.MapId);

            Assert.IsTrue(AreaBoundaryEvaluator.IsWalkable(context.Query, coord));
        }

        [Test]
        public void IsWalkable_ReturnsFalseForTurfWall()
        {
            TileMapTestUtilities.MapContext context = CreateMap();
            Vector3 center = new Vector3(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(context, center);
            PlaceTurf(context, center, TileObjectGenericType.Wall);

            TileCoord coord = context.Query.WorldToTile(center, context.Map.MapId);

            Assert.IsFalse(AreaBoundaryEvaluator.IsWalkable(context.Query, coord));
        }

        [Test]
        public void BlocksAreaExpansion_BlocksTurfDoorCrossing()
        {
            TileMapTestUtilities.MapContext context = CreateMap();
            Vector3 from = new Vector3(4, 0, 5);
            Vector3 door = new Vector3(5, 0, 5);
            Vector3 to = new Vector3(6, 0, 5);

            TileMapTestUtilities.PlacePlenum(context, from);
            TileMapTestUtilities.PlacePlenum(context, door);
            TileMapTestUtilities.PlacePlenum(context, to);
            PlaceTurf(context, door, TileObjectGenericType.Door);

            TileCoord fromCoord = context.Query.WorldToTile(from, context.Map.MapId);
            TileCoord doorCoord = context.Query.WorldToTile(door, context.Map.MapId);
            TileCoord toCoord = context.Query.WorldToTile(to, context.Map.MapId);

            Assert.IsTrue(AreaBoundaryEvaluator.BlocksAreaExpansion(
                fromCoord,
                doorCoord,
                context.Query,
                new AreaId(1),
                AreaId.None));

            Assert.IsTrue(AreaBoundaryEvaluator.BlocksAreaExpansion(
                doorCoord,
                toCoord,
                context.Query,
                new AreaId(1),
                AreaId.None));
        }

        private TileMapTestUtilities.MapContext CreateMap()
        {
            TileMapTestUtilities.EnsureTestAssetsRegistered();
            return TileMapTestUtilities.CreateContext(_instantiated);
        }

        private static void PlaceTurf(
            TileMapTestUtilities.MapContext context,
            Vector3 position,
            TileObjectGenericType genericType)
        {
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, $"Turf_{genericType}");
            turfSo.genericType = genericType;
            bool success = context.Map.PlaceTileObject(
                turfSo,
                position,
                Direction.North,
                skipBuildCheck: true,
                replaceExisting: false,
                skipAdjacency: true,
                out _);
            Assert.IsTrue(success, $"Expected turf placement to succeed at {position}.");
        }
    }
}
