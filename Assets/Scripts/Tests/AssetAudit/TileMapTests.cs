using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetAudit
{
    public class TileMapTests
    {
        #region Tests
        [Test, TestCaseSource(nameof(AllTileObjectSo))]
        public void EveryTileObjectSoHasAName(TileObjectSo tileObjectSo)
        {
            Assert.IsTrue(tileObjectSo.NameString != "", $"TileObjectSo '{tileObjectSo.name}' does not have name set.\n");
        }

        [Test, TestCaseSource(nameof(AllItemObjectSo))]
        public void EveryItemObjectSoHasAName(ItemObjectSo itemObjectSo)
        {
            Assert.IsTrue(itemObjectSo.NameString != "", $"ItemObjectSo '{itemObjectSo.name}' does not have name set.\n");
        }

        /// <summary>
        /// Test if every TileObjectSo has a size between 1 and 5
        /// </summary>
        [Test, TestCaseSource(nameof(AllTileObjectSo))]
        public void EveryTileHasRealisticSize(TileObjectSo tile)
        {
            bool invalidSize = tile.height <= 0 || tile.height > 5 || tile.width <= 0 || tile.width > 5;
            Assert.IsFalse(invalidSize, $"TileObjectSo '{tile.name}' has an unrealistic size of { tile.width},{ tile.height}.\n");
        }
        #endregion

        #region Helper functions

        public static TileObjectSo[] AllTileObjectSo()
        {
            return AssetAuditUtilities.AllTileObjectSo();
        }

        public static ItemObjectSo[] AllItemObjectSo()
        {
            return AssetAuditUtilities.AllItemObjectSo();
        }
    }

    #endregion
}
