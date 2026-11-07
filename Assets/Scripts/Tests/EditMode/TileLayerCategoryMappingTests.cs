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
                if (TileLayerCategoryMapping.IsItemsCategory(category)
                    || TileLayerCategoryMapping.IsFloorDecalsCategory(category))
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
            Assert.AreEqual(TileLayerCategory.Cables, wireCategory);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.FurnitureTop, out TileLayerCategory furnitureCategory));
            Assert.AreEqual(TileLayerCategory.Furniture, furnitureCategory);

            Assert.IsTrue(TileLayerCategoryMapping.IsFloorDecalsCategory(TileLayerCategory.FloorDecals));
            Assert.AreEqual(0, TileLayerCategoryMapping.GetLayers(TileLayerCategory.FloorDecals).Count);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.PipeMiddle, out TileLayerCategory pipeCategory));
            Assert.AreEqual(TileLayerCategory.GasPipes, pipeCategory);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.PipeManifold, out TileLayerCategory manifoldCategory));
            Assert.AreEqual(TileLayerCategory.GasPipes, manifoldCategory);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.Disposal, out TileLayerCategory disposalCategory));
            Assert.AreEqual(TileLayerCategory.Disposals, disposalCategory);

            Assert.IsTrue(TileLayerCategoryMapping.TryGetCategoryForLayer(TileLayer.Plenum, out TileLayerCategory plenumCategory));
            Assert.AreEqual(TileLayerCategory.Subfloor, plenumCategory);
        }

        [Test]
        public void EveryTileLayer_MapsToOneCategoryOnly()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                int count = 0;
                foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
                    foreach (TileLayer mapped in TileLayerCategoryMapping.GetLayers(category))
                        if (mapped == layer) count++;

                Assert.AreEqual(1, count, $"{layer} is in {count} layer groups");
            }
        }

        [Test]
        public void LowerGroups_UsePlainWords()
        {
            // creative-mode.md §2: plain words, matching the Lower tab's sub-tabs.
            Assert.AreEqual("Subfloor", TileLayerCategoryMapping.GetDisplayName(TileLayerCategory.Subfloor));
            Assert.AreEqual("Cables", TileLayerCategoryMapping.GetDisplayName(TileLayerCategory.Cables));
            Assert.AreEqual("Gas pipes", TileLayerCategoryMapping.GetDisplayName(TileLayerCategory.GasPipes));
            Assert.AreEqual("Disposals", TileLayerCategoryMapping.GetDisplayName(TileLayerCategory.Disposals));
        }

        [Test]
        public void TileLayer_DoesNotIncludeOverlays()
        {
            foreach (TileLayer layer in TileHelper.GetTileLayers())
                Assert.IsFalse(string.Equals(layer.ToString(), "Overlays", System.StringComparison.Ordinal));
        }
    }
}
