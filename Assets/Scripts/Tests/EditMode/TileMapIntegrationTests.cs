using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Tests;
using UnityEngine;

namespace EditorTests
{
    public class TileMapIntegrationTests : EditModeTest
    {
        private static readonly Vector3 Origin = new(5, 0, 5);

        [Test]
        public void MutationObserver_OnTilePlaced_WhenConstructionPlacesTile()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileMapTestUtilities.PlacePlenum(context, Origin);

            var observer = new TileMapTestUtilities.RecordingMutationObserver();
            context.Map.RegisterMutationObserver(observer);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            Assert.IsTrue(context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false).Success);

            Assert.AreEqual(1, observer.PlacedCount);
            Assert.AreEqual(new TileCoord(context.Map.MapId, 5, 5), observer.LastPlacedCoord);
        }

        [Test]
        public void MutationObserver_OnTileCleared_WhenConstructionClearsTile()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileMapTestUtilities.PlacePlenum(context, Origin);

            var observer = new TileMapTestUtilities.RecordingMutationObserver();
            context.Map.RegisterMutationObserver(observer);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false);

            ClearResult clearResult = context.Construction.TryClearTile(Origin, TileLayer.Turf, Direction.North);

            Assert.IsTrue(clearResult.Success);
            Assert.AreEqual(1, observer.ClearedCount);
            Assert.AreEqual(new TileCoord(context.Map.MapId, 5, 5), observer.LastClearedCoord);
            Assert.AreEqual(TileLayer.Turf, observer.LastClearedLayer);
            Assert.IsTrue(TileMapTestUtilities.IsLayerEmpty(context.Map, TileLayer.Turf, Origin));
        }

        [Test]
        public void MutationObserver_DoesNotFire_WhenPlacementFails()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            var observer = new TileMapTestUtilities.RecordingMutationObserver();
            context.Map.RegisterMutationObserver(observer);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            Assert.IsFalse(context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false).Success);

            Assert.AreEqual(0, observer.PlacedCount);
            Assert.AreEqual(0, observer.ClearedCount);
        }

        [Test]
        public void TryPlaceTile_SucceedsAfterPlenumPlaced()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileMapTestUtilities.PlacePlenum(context, Origin);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            PlaceResult result = context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(TileMapTestUtilities.IsLayerEmpty(context.Map, TileLayer.Turf, Origin));
        }

        [Test]
        public void TryPlaceTile_FailsWithoutPlenum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);

            PlaceResult result = context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(TileMapTestUtilities.IsLayerEmpty(context.Map, TileLayer.Turf, Origin));
        }

        [Test]
        public void TryPlaceTile_FailsWhenLayerOccupied()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileMapTestUtilities.PlacePlenum(context, Origin);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            Assert.IsTrue(context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false).Success);

            PlaceResult secondPlace = context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false);

            Assert.IsFalse(secondPlace.Success);
        }

        [Test]
        public void TryPreviewTile_MatchesPlacementEligibility()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);

            Assert.IsFalse(context.Construction.TryPreviewTile(turfSo, Origin, Direction.North, replaceExisting: false).CanBuild);

            TileMapTestUtilities.PlacePlenum(context, Origin);

            Assert.IsTrue(context.Construction.TryPreviewTile(turfSo, Origin, Direction.North, replaceExisting: false).CanBuild);
            Assert.IsTrue(context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false).Success);
        }

        [Test]
        public void TryFindFreeTile_ReturnsAdjacentEmptyTurfWithPlenum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            Vector3 centre = Origin;
            Vector3 neighbour = new(6, 0, 5);

            TileMapTestUtilities.PlacePlenum(context, centre);
            TileMapTestUtilities.PlacePlenum(context, neighbour);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            Assert.IsTrue(context.Construction.TryPlaceTile(turfSo, centre, Direction.North, replaceExisting: false).Success);

            TileCoord near = context.Query.WorldToTile(centre, context.Map.MapId);
            bool found = context.Query.TryFindFreeTile(near, TileLayer.Turf, out TileCoord free);

            Assert.IsTrue(found);
            Assert.AreEqual(new TileCoord(context.Map.MapId, 6, 5), free);
        }

        [Test]
        public void TryFindFreeTile_ReturnsFalseWhenNoPlenumBackedCandidateExists()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileMapTestUtilities.PlacePlenum(context, Origin);

            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);
            Assert.IsTrue(context.Construction.TryPlaceTile(turfSo, Origin, Direction.North, replaceExisting: false).Success);

            TileCoord near = context.Query.WorldToTile(Origin, context.Map.MapId);
            bool found = context.Query.TryFindFreeTile(near, TileLayer.Turf, out TileCoord free);

            Assert.IsFalse(found);
            Assert.AreEqual(default(TileCoord), free);
        }
    }
}
