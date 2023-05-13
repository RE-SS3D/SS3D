using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    public class TileMapTests
    {
        #region Tests
        /// <summary>
        /// Tests if an object can be built without a plenum by trying to build a wall on an empty tile;
        /// </summary>
        [Test]
        public void CannotBuildOnNonPlenum()
        {
            // SetUp
            TileObject[] tileObjects = CreateEmptyTileObjectArray();
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];
            TileObjectSo testSo = CreateTestWallSo();

            bool canBuild = BuildChecker.CanBuild(tileObjects, testSo, Direction.North, adjacentObjects, false);

            Assert.IsFalse(canBuild);
        }

        #endregion

        #region Helper functions
        private TileObject[] CreateEmptyTileObjectArray()
        {
            TileLayer[] layers = TileHelper.GetTileLayers();
            TileObject[] tileObjects = new TileObject[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                tileObjects[i] = new TileObject(layers[i], 0, 0);
            }

            return tileObjects;
        }

        private TileObjectSo CreateTestWallSo()
        {
            TileObjectSo testSo = (TileObjectSo)ScriptableObject.CreateInstance(typeof(TileObjectSo));
            testSo.nameString = "wall_test";
            testSo.width = 1;
            testSo.height = 1;
            testSo.layer = TileLayer.Turf;
            testSo.genericType = TileObjectGenericType.Wall;

            return testSo;
        }
    }

    #endregion
}
