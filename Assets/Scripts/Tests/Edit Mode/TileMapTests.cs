using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Text;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace EditorTests
{
    public class TileMapTests
    {
        #region Tests
        /// <summary>
        /// Tests if an object can be built without a plenum by trying to build a disposal pipe on an empty tile;
        /// </summary>
        [Test]
        public void CannotBuildOnNonPlenum()
        {
            // SetUp
            ITileLocation[] tileLocations = CreateEmptyTileObjectArray();
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];
            TileObjectSo disposalPipeSo = CreateTestSo("disposal_pipe_test", 1, 1,
                TileLayer.Disposal, TileObjectGenericType.Pipe);

            bool canBuild = BuildChecker.CanBuild(tileLocations, disposalPipeSo, Direction.North,
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

        private TileObjectSo CreateTestSo(string name, int width, int height,
            TileLayer layer, TileObjectGenericType genericType)
        {
            TileObjectSo testSo = (TileObjectSo)ScriptableObject.CreateInstance(typeof(TileObjectSo));
            testSo.nameString = "wall_test";
            testSo.width = 1;
            testSo.height = 1;
            testSo.layer = TileLayer.Turf;
            testSo.genericType = TileObjectGenericType.Pipe;

            return testSo;
        }
    }

    #endregion
}
