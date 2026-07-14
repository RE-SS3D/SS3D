using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections.Generic;

namespace SS3D.Tests.EditMode
{
    public sealed class TileLayerCategoryMappingTests
    {
        [Test]
        public void AllTileLayers_AreMappedExactlyOnce()
        {
            var mappedLayers = new HashSet<TileLayer>();

            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                if (TileLayerCategoryMapping.IsItemsCategory(category))
                    continue;

                foreach (TileLayer layer in TileLayerCategoryMapping.GetLayers(category))
                    mappedLayers.Add(layer);
            }

            foreach (TileLayer layer in TileHelper.GetTileLayers())
                Assert.IsTrue(mappedLayers.Contains(layer), $"Missing mapping for {layer}");
        }

        [Test]
        public void DropdownIndex_RoundTripsAllCategories()
        {
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                int index = TileLayerCategoryMapping.ToDropdownIndex(category);
                Assert.AreEqual(category, TileLayerCategoryMapping.FromDropdownIndex(index));
            }
        }

        [Test]
        public void TryGetCategoryForLayer_ReturnsExpectedGroups()
        {
            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.Wire, out TileLayerCategory wireCategory));
            Assert.AreEqual(TileLayerCategory.WiresAndPipes, wireCategory);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.FurnitureTop, out TileLayerCategory furnitureCategory));
            Assert.AreEqual(TileLayerCategory.Furniture, furnitureCategory);
        }
    }
}
