using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class GratingUnderTurfMigrationTests
    {
        private static readonly Vector2Int Cell = new(3, 4);
        private static readonly Vector2Int OtherCell = new(5, 6);

        [TestCase("Catwalk")]
        [TestCase("Lattice")]
        public void GratingUnderWall_IsConverted(string gratingName)
        {
            HashSet<Vector2Int> cells = GratingUnderTurfMigration.FindCellsToConvert(new[]
            {
                (Cell, So(TileLayer.Plenum, gratingName)),
                (Cell, So(TileLayer.Turf, "SteelWall", TileObjectGenericType.Wall)),
            });

            CollectionAssert.AreEquivalent(new[] { Cell }, cells);
        }

        [Test]
        public void CatwalkUnderFloor_IsConverted()
        {
            HashSet<Vector2Int> cells = GratingUnderTurfMigration.FindCellsToConvert(new[]
            {
                (Cell, So(TileLayer.Turf, "SteelFloor")),
                (Cell, So(TileLayer.Plenum, "Catwalk")),
            });

            CollectionAssert.AreEquivalent(new[] { Cell }, cells);
        }

        [Test]
        public void CatwalkWithOnlyWire_IsKept()
        {
            HashSet<Vector2Int> cells = GratingUnderTurfMigration.FindCellsToConvert(new[]
            {
                (Cell, So(TileLayer.Plenum, "Catwalk")),
                (Cell, So(TileLayer.Wire, "Cable")),
            });

            Assert.IsEmpty(cells);
        }

        [Test]
        public void TurfOnNeighbouringCell_DoesNotConvertCatwalk()
        {
            HashSet<Vector2Int> cells = GratingUnderTurfMigration.FindCellsToConvert(new[]
            {
                (Cell, So(TileLayer.Plenum, "Catwalk")),
                (OtherCell, So(TileLayer.Plenum, "Plenum")),
                (OtherCell, So(TileLayer.Turf, "SteelWall", TileObjectGenericType.Wall)),
            });

            Assert.IsEmpty(cells);
        }

        [Test]
        public void PlenumUnderWall_IsNotConverted()
        {
            HashSet<Vector2Int> cells = GratingUnderTurfMigration.FindCellsToConvert(new[]
            {
                (Cell, So(TileLayer.Plenum, "Plenum")),
                (Cell, So(TileLayer.Turf, "SteelWall", TileObjectGenericType.Wall)),
            });

            Assert.IsEmpty(cells);
        }

        private static TileObjectSo So(TileLayer layer, string name,
            TileObjectGenericType genericType = TileObjectGenericType.Floor)
        {
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(layer, name);
            so.genericType = genericType;
            return so;
        }
    }
}
