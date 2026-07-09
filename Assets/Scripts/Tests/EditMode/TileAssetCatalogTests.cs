using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class TileAssetCatalogTests
    {
        [Test]
        public void Catalog_AssignsIndicesSortedByName()
        {
            TileObjectSo zebra = CreateNamedSo("ZebraFloor");
            TileObjectSo alpha = CreateNamedSo("AlphaFloor");
            TileObjectSo middle = CreateNamedSo("MiddleFloor");

            TileAssetCatalog catalog = new();
            catalog.Build(new List<GenericObjectSo> { zebra, alpha, middle });

            Assert.AreEqual(0, catalog.TryGetAssetId(alpha));
            Assert.AreEqual(1, catalog.TryGetAssetId(middle));
            Assert.AreEqual(2, catalog.TryGetAssetId(zebra));
        }

        [Test]
        public void Catalog_RoundTripsAssetLookup()
        {
            TileObjectSo floor = CreateNamedSo("TestFloor");

            TileAssetCatalog catalog = new();
            catalog.Build(new List<GenericObjectSo> { floor });

            ushort id = catalog.TryGetAssetId(floor);

            Assert.AreNotEqual(TileAssetCatalog.InvalidAssetId, id);
            Assert.AreSame(floor, catalog.GetAsset(id));
        }

        [Test]
        public void Catalog_ReturnsInvalidForUnknownAsset()
        {
            TileAssetCatalog catalog = new();
            catalog.Build(new List<GenericObjectSo> { CreateNamedSo("Known") });

            Assert.AreEqual(TileAssetCatalog.InvalidAssetId, catalog.TryGetAssetId(CreateNamedSo("Unknown")));
            Assert.IsNull(catalog.GetAsset(TileAssetCatalog.InvalidAssetId));
        }

        private static TileObjectSo CreateNamedSo(string name)
        {
            ObjectAssetReference prefabRef = ScriptableObject.CreateInstance<ObjectAssetReference>();
            prefabRef.name = name;

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.PrefabAsset = prefabRef;
            return so;
        }
    }
}
