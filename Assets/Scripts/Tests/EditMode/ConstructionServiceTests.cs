using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Tests;
using UnityEngine;

namespace EditorTests
{
    public class ConstructionServiceTests : EditModeTest
    {
        [Test]
        public void TryPreviewTile_ReturnsFalseWhenChunkMissing()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);

            PreviewResult preview = context.Construction.TryPreviewTile(turfSo, new Vector3(2, 0, 2), Direction.North, false);

            Assert.IsFalse(preview.CanBuild);
        }

        [Test]
        public void TryPreviewTile_ReturnsTrueWhenPlenumExists()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf);

            TileMapTestUtilities.PlacePlenum(context, new Vector3(3, 0, 3));
            PreviewResult preview = context.Construction.TryPreviewTile(turfSo, new Vector3(3, 0, 3), Direction.North, false);

            Assert.IsTrue(preview.CanBuild);
        }
    }
}
