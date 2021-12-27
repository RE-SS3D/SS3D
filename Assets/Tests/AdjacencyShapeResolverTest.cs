using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Engine.Tile.TileRework.Connections;
using SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes;
using SS3D.Engine.Tiles;

namespace SS3D.Tests
{
    public class AdjacencyShapeResolverTest
    {
        private readonly AdjacencyData existingConnection = new AdjacencyData("", "", true);
        private readonly AdjacencyData missingConnection = new AdjacencyData("", "", false);

        [Test]
        public void SimpleShapeShouldReturn0WhenNoConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }

        [Test]
        public void SimpleShapeShouldReturn0WhenHasOnlyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }
        
        [Test]
        public void SimpleShapeShouldReturnUWhenSingleOrthogonalConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();

            adjacencyMap.SetConnection(Direction.North, existingConnection);
            AdjacencyShape resultN = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.North, missingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            AdjacencyShape resultE = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.East, missingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            AdjacencyShape resultS = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.South, missingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            AdjacencyShape resultW = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(resultN == AdjacencyShape.U);
            Assert.IsTrue(resultE == AdjacencyShape.U);
            Assert.IsTrue(resultS == AdjacencyShape.U);
            Assert.IsTrue(resultW == AdjacencyShape.U);
        }
        
        [Test]
        public void SimpleShapeShouldReturnUWhenHasSingleOrthogonalAndManyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.U);
        }
        
        [Test]
        public void SimpleShapeShouldReturnIWhenHasTwoOppositeOrthogonalConnections()
        {
            AdjacencyMap adjacencyMapNorthSouth = new AdjacencyMap();
            adjacencyMapNorthSouth.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthSouth.SetConnection(Direction.South, existingConnection);
            AdjacencyMap adjacencyMapEastWest = new AdjacencyMap();
            adjacencyMapEastWest.SetConnection(Direction.East, existingConnection);
            adjacencyMapEastWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthSouth = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapNorthSouth);
            AdjacencyShape resultEastWest = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapEastWest);

            Assert.IsTrue(resultNorthSouth == AdjacencyShape.I);
            Assert.IsTrue(resultEastWest == AdjacencyShape.I);
        }
        
        [Test]
        public void SimpleShapeShouldReturnLWhenHasTwoAdjacentOrthogonalConnections()
        {
            AdjacencyMap adjacencyMapNorthEast = new AdjacencyMap();
            adjacencyMapNorthEast.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthEast = new AdjacencyMap();
            adjacencyMapSouthEast.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthWest = new AdjacencyMap();
            adjacencyMapSouthWest.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.West, existingConnection);
            AdjacencyMap adjacencyMapNorthWest = new AdjacencyMap();
            adjacencyMapNorthWest.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthEast = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapNorthEast);
            AdjacencyShape resultSouthEast = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapSouthEast);
            AdjacencyShape resultSouthWest = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapSouthWest);
            AdjacencyShape resultNorthWest = AdjacencyShapeResolver.GetSimpleShape(adjacencyMapNorthWest);

            Assert.IsTrue(resultNorthEast == AdjacencyShape.L);
            Assert.IsTrue(resultSouthEast == AdjacencyShape.L);
            Assert.IsTrue(resultSouthWest == AdjacencyShape.L);
            Assert.IsTrue(resultNorthWest == AdjacencyShape.L);
        }

        [Test]
        public void SimpleShapeShouldReturnTWhenHasThreeOrthogonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.T);
        }
        
        [Test]
        public void SimpleShapeShouldReturnTWhenHasThreeOrthogonalAndManyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.T);
        }
        
        [Test]
        public void SimpleShapeShouldReturnXWhenHasFourOrthogonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.X);
        }
        
        [Test]
        public void AdvancedShapeShouldReturn0WhenNoConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }

        [Test]
        public void AdvancedShapeShouldReturn0WhenHasOnlyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnUWhenSingleOrthogonalConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();

            adjacencyMap.SetConnection(Direction.North, existingConnection);
            AdjacencyShape resultN = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.North, missingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            AdjacencyShape resultE = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.East, missingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            AdjacencyShape resultS = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
            adjacencyMap.SetConnection(Direction.South, missingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            AdjacencyShape resultW = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(resultN == AdjacencyShape.U);
            Assert.IsTrue(resultE == AdjacencyShape.U);
            Assert.IsTrue(resultS == AdjacencyShape.U);
            Assert.IsTrue(resultW == AdjacencyShape.U);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnUWhenHasSingleOrthogonalAndManyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.U);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnIWhenHasTwoOppositeOrthogonalConnections()
        {
            AdjacencyMap adjacencyMapNorthSouth = new AdjacencyMap();
            adjacencyMapNorthSouth.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthSouth.SetConnection(Direction.South, existingConnection);
            AdjacencyMap adjacencyMapEastWest = new AdjacencyMap();
            adjacencyMapEastWest.SetConnection(Direction.East, existingConnection);
            adjacencyMapEastWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthSouth = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthSouth);
            AdjacencyShape resultEastWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapEastWest);

            Assert.IsTrue(resultNorthSouth == AdjacencyShape.I);
            Assert.IsTrue(resultEastWest == AdjacencyShape.I);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnLSingleWhenHasThreeConsecutiveConnections()
        {
            AdjacencyMap adjacencyMapNorthEast = new AdjacencyMap();
            adjacencyMapNorthEast.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthEast = new AdjacencyMap();
            adjacencyMapSouthEast.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthWest = new AdjacencyMap();
            adjacencyMapSouthWest.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.West, existingConnection);
            AdjacencyMap adjacencyMapNorthWest = new AdjacencyMap();
            adjacencyMapNorthWest.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthEast);
            AdjacencyShape resultSouthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthEast);
            AdjacencyShape resultSouthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthWest);
            AdjacencyShape resultNorthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthWest);

            Assert.IsTrue(resultNorthEast == AdjacencyShape.LSingle);
            Assert.IsTrue(resultSouthEast == AdjacencyShape.LSingle);
            Assert.IsTrue(resultSouthWest == AdjacencyShape.LSingle);
            Assert.IsTrue(resultNorthWest == AdjacencyShape.LSingle);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnLNoneWhenHasTwoAdjacentOrthogonalConnections()
        {
            AdjacencyMap adjacencyMapNorthEast = new AdjacencyMap();
            adjacencyMapNorthEast.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthEast = new AdjacencyMap();
            adjacencyMapSouthEast.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.East, existingConnection);
            AdjacencyMap adjacencyMapSouthWest = new AdjacencyMap();
            adjacencyMapSouthWest.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.West, existingConnection);
            AdjacencyMap adjacencyMapNorthWest = new AdjacencyMap();
            adjacencyMapNorthWest.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthEast);
            AdjacencyShape resultSouthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthEast);
            AdjacencyShape resultSouthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthWest);
            AdjacencyShape resultNorthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthWest);

            Assert.IsTrue(resultNorthEast == AdjacencyShape.LNone);
            Assert.IsTrue(resultSouthEast == AdjacencyShape.LNone);
            Assert.IsTrue(resultSouthWest == AdjacencyShape.LNone);
            Assert.IsTrue(resultNorthWest == AdjacencyShape.LNone);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTNoneWhenHasThreeOrthogonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TNone);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTNoneWhenHasThreeOrthogonalAndOneOppositeDiagonalConnection()
        {
            AdjacencyMap adjacencyMapL = new AdjacencyMap();
            adjacencyMapL.SetConnection(Direction.North, existingConnection);
            adjacencyMapL.SetConnection(Direction.East, existingConnection);
            adjacencyMapL.SetConnection(Direction.West, existingConnection);
            adjacencyMapL.SetConnection(Direction.SouthWest, existingConnection);
            AdjacencyMap adjacencyMapR = new AdjacencyMap();
            adjacencyMapR.SetConnection(Direction.North, existingConnection);
            adjacencyMapR.SetConnection(Direction.East, existingConnection);
            adjacencyMapR.SetConnection(Direction.West, existingConnection);
            adjacencyMapR.SetConnection(Direction.SouthEast, existingConnection);

            AdjacencyShape resultL = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapL);
            AdjacencyShape resultR = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapR);

            Assert.IsTrue(resultL == AdjacencyShape.TNone);
            Assert.IsTrue(resultR == AdjacencyShape.TNone);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTNoneWhenHasThreeOrthogonalAndBothOppositeDiagonalConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TNone);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTSingleLeftWhenHasThreeOrthogonalAndLeftDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TSingleLeft);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTSingleRightWhenHasThreeOrthogonalAndRightDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TSingleRight);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnTDoubleWhenHasThreeOrthogonalAndBothDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TDouble);
        }

        [Test]
        public void AdvancedShapeShouldReturnXNoneWhenHasFourOrthogonalAndNoDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.XNone);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnXSingleWhenHasFourOrthogonalAndOneDiagonalConnection()
        {
            AdjacencyMap adjacencyMapNorthEast = new AdjacencyMap();
            adjacencyMapNorthEast.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.East, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.South, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.West, existingConnection);
            adjacencyMapNorthEast.SetConnection(Direction.NorthEast, existingConnection);
            AdjacencyMap adjacencyMapNorthWest = new AdjacencyMap();
            adjacencyMapNorthWest.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.East, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.South, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.West, existingConnection);
            adjacencyMapNorthWest.SetConnection(Direction.NorthWest, existingConnection);
            AdjacencyMap adjacencyMapSouthEast = new AdjacencyMap();
            adjacencyMapSouthEast.SetConnection(Direction.North, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.East, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.West, existingConnection);
            adjacencyMapSouthEast.SetConnection(Direction.SouthEast, existingConnection);
            AdjacencyMap adjacencyMapSouthWest = new AdjacencyMap();
            adjacencyMapSouthWest.SetConnection(Direction.North, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.East, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.West, existingConnection);
            adjacencyMapSouthWest.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape resultNorthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthEast);
            AdjacencyShape resultNorthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapNorthWest);
            AdjacencyShape resultSouthEast = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthEast);
            AdjacencyShape resultSouthWest = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMapSouthWest);

            Assert.IsTrue(resultNorthEast == AdjacencyShape.XSingle);
            Assert.IsTrue(resultNorthWest == AdjacencyShape.XSingle);
            Assert.IsTrue(resultSouthEast == AdjacencyShape.XSingle);
            Assert.IsTrue(resultSouthWest == AdjacencyShape.XSingle);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnXOppositeWhenHasFourOrthogonalAndTwoOppositeDiagonalConnections()
        {
            AdjacencyMap adjacencyMap1 = new AdjacencyMap();
            adjacencyMap1.SetConnection(Direction.North, existingConnection);
            adjacencyMap1.SetConnection(Direction.East, existingConnection);
            adjacencyMap1.SetConnection(Direction.South, existingConnection);
            adjacencyMap1.SetConnection(Direction.West, existingConnection);
            adjacencyMap1.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap1.SetConnection(Direction.SouthWest, existingConnection);
            AdjacencyMap adjacencyMap2 = new AdjacencyMap();
            adjacencyMap2.SetConnection(Direction.North, existingConnection);
            adjacencyMap2.SetConnection(Direction.East, existingConnection);
            adjacencyMap2.SetConnection(Direction.South, existingConnection);
            adjacencyMap2.SetConnection(Direction.West, existingConnection);
            adjacencyMap2.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMap2.SetConnection(Direction.SouthEast, existingConnection);

            AdjacencyShape result1 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap1);
            AdjacencyShape result2 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap2);

            Assert.IsTrue(result1 == AdjacencyShape.XOpposite);
            Assert.IsTrue(result2 == AdjacencyShape.XOpposite);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnXSideWhenHasFourOrthogonalAndTwoAdjacentDiagonalConnections()
        {
            AdjacencyMap adjacencyMap1 = new AdjacencyMap();
            adjacencyMap1.SetConnection(Direction.North, existingConnection);
            adjacencyMap1.SetConnection(Direction.East, existingConnection);
            adjacencyMap1.SetConnection(Direction.South, existingConnection);
            adjacencyMap1.SetConnection(Direction.West, existingConnection);
            adjacencyMap1.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap1.SetConnection(Direction.SouthEast, existingConnection);
            AdjacencyMap adjacencyMap2 = new AdjacencyMap();
            adjacencyMap2.SetConnection(Direction.North, existingConnection);
            adjacencyMap2.SetConnection(Direction.East, existingConnection);
            adjacencyMap2.SetConnection(Direction.South, existingConnection);
            adjacencyMap2.SetConnection(Direction.West, existingConnection);
            adjacencyMap2.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap2.SetConnection(Direction.SouthWest, existingConnection);
            AdjacencyMap adjacencyMap3 = new AdjacencyMap();
            adjacencyMap3.SetConnection(Direction.North, existingConnection);
            adjacencyMap3.SetConnection(Direction.East, existingConnection);
            adjacencyMap3.SetConnection(Direction.South, existingConnection);
            adjacencyMap3.SetConnection(Direction.West, existingConnection);
            adjacencyMap3.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap3.SetConnection(Direction.NorthWest, existingConnection);
            AdjacencyMap adjacencyMap4 = new AdjacencyMap();
            adjacencyMap4.SetConnection(Direction.North, existingConnection);
            adjacencyMap4.SetConnection(Direction.East, existingConnection);
            adjacencyMap4.SetConnection(Direction.South, existingConnection);
            adjacencyMap4.SetConnection(Direction.West, existingConnection);
            adjacencyMap4.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMap4.SetConnection(Direction.NorthEast, existingConnection);

            AdjacencyShape result1 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap1);
            AdjacencyShape result2 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap2);
            AdjacencyShape result3 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap3);
            AdjacencyShape result4 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap4);

            Assert.IsTrue(result1 == AdjacencyShape.XSide);
            Assert.IsTrue(result2 == AdjacencyShape.XSide);
            Assert.IsTrue(result3 == AdjacencyShape.XSide);
            Assert.IsTrue(result4 == AdjacencyShape.XSide);
        }
        
        [Test]
        public void AdvancedShapeShouldReturnXTripleWhenHasFourOrthogonalAndThreeDiagonalConnections()
        {
            AdjacencyMap adjacencyMap1 = new AdjacencyMap();
            adjacencyMap1.SetConnection(Direction.North, existingConnection);
            adjacencyMap1.SetConnection(Direction.East, existingConnection);
            adjacencyMap1.SetConnection(Direction.South, existingConnection);
            adjacencyMap1.SetConnection(Direction.West, existingConnection);
            adjacencyMap1.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMap1.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap1.SetConnection(Direction.SouthEast, existingConnection);
            AdjacencyMap adjacencyMap2 = new AdjacencyMap();
            adjacencyMap2.SetConnection(Direction.North, existingConnection);
            adjacencyMap2.SetConnection(Direction.East, existingConnection);
            adjacencyMap2.SetConnection(Direction.South, existingConnection);
            adjacencyMap2.SetConnection(Direction.West, existingConnection);
            adjacencyMap2.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap2.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap2.SetConnection(Direction.SouthEast, existingConnection);
            AdjacencyMap adjacencyMap3 = new AdjacencyMap();
            adjacencyMap3.SetConnection(Direction.North, existingConnection);
            adjacencyMap3.SetConnection(Direction.East, existingConnection);
            adjacencyMap3.SetConnection(Direction.South, existingConnection);
            adjacencyMap3.SetConnection(Direction.West, existingConnection);
            adjacencyMap3.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap3.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap3.SetConnection(Direction.NorthWest, existingConnection);
            AdjacencyMap adjacencyMap4 = new AdjacencyMap();
            adjacencyMap4.SetConnection(Direction.North, existingConnection);
            adjacencyMap4.SetConnection(Direction.East, existingConnection);
            adjacencyMap4.SetConnection(Direction.South, existingConnection);
            adjacencyMap4.SetConnection(Direction.West, existingConnection);
            adjacencyMap4.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap4.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMap4.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape result1 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap1);
            AdjacencyShape result2 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap2);
            AdjacencyShape result3 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap3);
            AdjacencyShape result4 = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap4);

            Assert.IsTrue(result1 == AdjacencyShape.XTriple);
            Assert.IsTrue(result2 == AdjacencyShape.XTriple);
            Assert.IsTrue(result3 == AdjacencyShape.XTriple);
            Assert.IsTrue(result4 == AdjacencyShape.XTriple);
        }

        [Test]
        public void AdvancedShapeShouldReturnXQuadWhenHasFourOrthogonalAndDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
            
            Assert.IsTrue(result == AdjacencyShape.XQuad);
        }
        
        [Test]
        public void OffsetShapeShouldReturn0WhenNoConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            AdjacencyShape result = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }

        [Test]
        public void OffsetShapeShouldReturn0WhenHasOnlyDiagonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.O);
        }
        
        [Test]
        public void OffsetShapeShouldReturnUNorthWhenHasSingleConnectionNorthOrEast()
        {
            AdjacencyMap adjacencyMapNorth = new AdjacencyMap();
            adjacencyMapNorth.SetConnection(Direction.North, existingConnection);
            AdjacencyMap adjacencyMapEast = new AdjacencyMap();
            adjacencyMapEast.SetConnection(Direction.East, existingConnection);

            AdjacencyShape resultNorth = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapNorth);
            AdjacencyShape resultEast = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapEast);

            Assert.IsTrue(resultNorth == AdjacencyShape.UNorth);
            Assert.IsTrue(resultEast == AdjacencyShape.UNorth);
        }
        
        [Test]
        public void OffsetShapeShouldReturnUSouthWhenHasSingleConnectionSouthOrWest()
        {
            AdjacencyMap adjacencyMapSouth = new AdjacencyMap();
            adjacencyMapSouth.SetConnection(Direction.South, existingConnection);
            AdjacencyMap adjacencyMapWest = new AdjacencyMap();
            adjacencyMapWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultSouth = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapSouth);
            AdjacencyShape resultWest = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapWest);

            Assert.IsTrue(resultSouth == AdjacencyShape.USouth);
            Assert.IsTrue(resultWest == AdjacencyShape.USouth);
        }
        
        [Test]
        public void OffsetShapeShouldReturnUNorthWhenHasOrthogonalConnectionNorthOrEastAndAnyDiagonals()
        {
            AdjacencyMap adjacencyMapNorth = new AdjacencyMap();
            adjacencyMapNorth.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorth.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMapNorth.SetConnection(Direction.SouthEast, existingConnection);
            AdjacencyMap adjacencyMapEast = new AdjacencyMap();
            adjacencyMapEast.SetConnection(Direction.East, existingConnection);
            adjacencyMapEast.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMapEast.SetConnection(Direction.SouthWest, existingConnection);

            AdjacencyShape resultNorth = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapNorth);
            AdjacencyShape resultEast = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapEast);

            Assert.IsTrue(resultNorth == AdjacencyShape.UNorth);
            Assert.IsTrue(resultEast == AdjacencyShape.UNorth);
        }
        
        [Test]
        public void OffsetShapeShouldReturnUSouthWhenHasOrthogonalConnectionSouthOrWestAndAnyDiagonals()
        {
            AdjacencyMap adjacencyMapSouth = new AdjacencyMap();
            adjacencyMapSouth.SetConnection(Direction.South, existingConnection);
            adjacencyMapSouth.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMapSouth.SetConnection(Direction.SouthWest, existingConnection);
            AdjacencyMap adjacencyMapWest = new AdjacencyMap();
            adjacencyMapWest.SetConnection(Direction.West, existingConnection);
            adjacencyMapWest.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMapWest.SetConnection(Direction.SouthEast, existingConnection);

            AdjacencyShape resultSouth = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapSouth);
            AdjacencyShape resultWest = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapWest);

            Assert.IsTrue(resultSouth == AdjacencyShape.USouth);
            Assert.IsTrue(resultWest == AdjacencyShape.USouth);
        }
        
        [Test]
        public void OffsetShapeShouldReturnIWhenHasTwoOppositeOrthogonalConnections()
        {
            AdjacencyMap adjacencyMapNorthSouth = new AdjacencyMap();
            adjacencyMapNorthSouth.SetConnection(Direction.North, existingConnection);
            adjacencyMapNorthSouth.SetConnection(Direction.South, existingConnection);
            AdjacencyMap adjacencyMapEastWest = new AdjacencyMap();
            adjacencyMapEastWest.SetConnection(Direction.East, existingConnection);
            adjacencyMapEastWest.SetConnection(Direction.West, existingConnection);

            AdjacencyShape resultNorthSouth = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapNorthSouth);
            AdjacencyShape resultEastWest = AdjacencyShapeResolver.GetOffsetShape(adjacencyMapEastWest);

            Assert.IsTrue(resultNorthSouth == AdjacencyShape.I);
            Assert.IsTrue(resultEastWest == AdjacencyShape.I);
        }
        
        [Test]
        public void OffsetShapeShouldReturnLSouthWestWhenHasAllNorthWestConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.LSouthWest);
        }
        
        [Test]
        public void OffsetShapeShouldReturnLSouthEastWhenHasAllSouthWestConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.LSouthEast);
        }
        
        [Test]
        public void OffsetShapeShouldReturnLNorthEastWhenHasAllSouthEastConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.LNorthEast);
        }
        
        [Test]
        public void OffsetShapeShouldReturnLNorthWestWhenHasAllNorthEastConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.LNorthWest);
        }

        [Test]
        public void OffsetShapeShouldReturnTNorthSouthEastWhenMissingNorthConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TNorthSouthEast);
        }
        
        [Test]
        public void OffsetShapeShouldReturnTSouthWestEastWhenMissingEastConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TSouthWestEast);
        }
        
        [Test]
        public void OffsetShapeShouldReturnTNorthSouthWestWhenMissingSouthConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TNorthSouthWest);
        }
        
        [Test]
        public void OffsetShapeShouldReturnTNorthEastWestWhenMissingWestConnection()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);

            AdjacencyShape result= AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.TNorthEastWest);
        }
        
        [Test]
        public void OffsetShapeShouldReturnXWhenHasFourOrthogonalConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.X);
        }
        
        [Test]
        public void OffsetShapeShouldReturnXWhenHasAllConnections()
        {
            AdjacencyMap adjacencyMap = new AdjacencyMap();
            adjacencyMap.SetConnection(Direction.North, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.East, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthEast, existingConnection);
            adjacencyMap.SetConnection(Direction.South, existingConnection);
            adjacencyMap.SetConnection(Direction.SouthWest, existingConnection);
            adjacencyMap.SetConnection(Direction.West, existingConnection);
            adjacencyMap.SetConnection(Direction.NorthWest, existingConnection);

            AdjacencyShape result = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);

            Assert.IsTrue(result == AdjacencyShape.X);
        }
    }
}
