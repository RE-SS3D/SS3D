using NUnit.Framework;
using SS3D.Data.AssetDatabases;
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
            TileMap map = TileMap.Create("ConstructionPreviewTest");
            instantiated.Add(map.gameObject);

            var query = new TileQueryService(map);
            var construction = new ConstructionService(map, query);
            TileObjectSo turfSo = CreateTestSo(TileLayer.Turf);

            PreviewResult preview = construction.TryPreviewTile(turfSo, new Vector3(2, 0, 2), Direction.North, false);

            Assert.IsFalse(preview.CanBuild);
        }

        [Test]
        public void TryPreviewTile_ReturnsTrueWhenPlenumExists()
        {
            TileMap map = TileMap.Create("ConstructionPreviewValidTest");
            instantiated.Add(map.gameObject);

            var query = new TileQueryService(map);
            var construction = new ConstructionService(map, query);
            TileObjectSo turfSo = CreateTestSo(TileLayer.Turf);

            map.GetOrCreateTileLocation(TileLayer.Plenum, new Vector3(3, 0, 3));
            PreviewResult preview = construction.TryPreviewTile(turfSo, new Vector3(3, 0, 3), Direction.North, false);

            Assert.IsTrue(preview.CanBuild);
        }

        private static TileObjectSo CreateTestSo(TileLayer layer)
        {
            ObjectAssetReference prefabRef = ScriptableObject.CreateInstance<ObjectAssetReference>();
            prefabRef.name = $"ConstructionTest_{layer}";

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.PrefabAsset = prefabRef;
            so.width = 1;
            so.height = 1;
            so.genericType = TileObjectGenericType.Floor;
            so.specificType = TileObjectSpecificType.None;
            so.layer = layer;
            return so;
        }
    }
}
