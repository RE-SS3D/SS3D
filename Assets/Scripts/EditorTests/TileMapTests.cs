using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EditorTests.TileMaps
{
    public class TileMapTests
    {
        TileSystem _tileSystem;

        TileObjectSo[] _tileObjects;
        ItemObjectSo[] _itemObjects;

        #region Test set up
        [SetUp]
        public void SetUp()
        {
            LoadAllTileObjectSo();
            LoadAllItemObjectSo();
        }

        [TearDown]
        public void TearDown()
        {
            
        }
        #endregion

        #region Tests

        /// <summary>
        /// Test if every tile related scriptableobject has a name assigned.
        /// </summary>
        [Test]
        public void EverySoObjectHasName()
        {
            StringBuilder sb = new();
            bool emptyName = false;

            foreach (TileObjectSo tile in _tileObjects)
            {
                bool foundEmpty = tile.nameString == "";
                emptyName |= foundEmpty;

                if (foundEmpty)
                    sb.Append($"-> TileObjectSo '{tile.name}' does not have name set.\n");
            }

            foreach (ItemObjectSo item in _itemObjects)
            {
                bool foundEmpty = item.nameString == "";
                emptyName |= foundEmpty;

                if (foundEmpty)
                    sb.Append($"-> ItemObjectSo '{item.name}' does not have name set.\n");
            }

            Assert.IsFalse(emptyName, sb.ToString());
        }

        /// <summary>
        /// Test if every TileObjectSo has a size between 1 and 5
        /// </summary>
        [Test]
        public void EveryTileHasRealisticSize()
        {
            StringBuilder sb = new();
            bool foundOddSize = false;

            foreach (TileObjectSo tile in _tileObjects)
            {
                bool invalidSize = tile.height <= 0 || tile.height > 5;
                invalidSize |= tile.width <= 0 || tile.width > 5;

                foundOddSize |= invalidSize;

                if (invalidSize)
                    sb.Append($"-> TileObjectSo '{tile.name}' has an unrealistic size of {tile.width},{tile.height}.\n");
            }

            Assert.IsFalse(foundOddSize, sb.ToString());
        }

        /// <summary>
        /// Tests if an object can be build without a plenum by trying to build a wall on an empty tile;
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

        private void LoadAllTileObjectSo()
        {
            // Find all the prefabs in the project hierarchy (i.e. NOT in a scene)
            string[] guids = AssetDatabase.FindAssets("t:TileObjectSo");

            // Create our array of prefabs
            _tileObjects = new TileObjectSo[guids.Length];

            // Populate the array
            for (int i = 0; i < guids.Length; i++)
            {
                _tileObjects[i] = AssetDatabase.LoadAssetAtPath<TileObjectSo>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
        }

        private void LoadAllItemObjectSo()
        {
            // Find all the prefabs in the project hierarchy (i.e. NOT in a scene)
            string[] guids = AssetDatabase.FindAssets("t:ItemObjectSo");

            // Create our array of prefabs
            _itemObjects = new ItemObjectSo[guids.Length];

            // Populate the array
            for (int i = 0; i < guids.Length; i++)
            {
                _itemObjects[i] = AssetDatabase.LoadAssetAtPath<ItemObjectSo>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
        }

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
