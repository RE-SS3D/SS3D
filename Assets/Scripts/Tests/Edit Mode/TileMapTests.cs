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
            ITileLocation[] tileLocations = CreateEmptyTileObjectArray();
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];
            TileObjectSo testSo = CreateTestWallSo();

            bool canBuild = BuildChecker.CanBuild(tileLocations, testSo, Direction.North,
                new Vector3(0,0,0), adjacentObjects, false);

            Assert.IsFalse(canBuild);
        }

        #endregion

        #region Helper functions
        private ITileLocation[] CreateEmptyTileObjectArray()
        {
            TileLayer[] layers = TileHelper.GetTileLayers();
            ITileLocation[] tileLocations = new ITileLocation[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                tileLocations[i] = TileHelper.CreateTileLocation(layers[i], 0,0);
            }

            return tileLocations;
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
