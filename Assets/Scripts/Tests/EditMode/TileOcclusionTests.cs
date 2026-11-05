using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class TileOcclusionTests
    {
        private GameObject _systemHolder;
        private TileOcclusionSubSystem _subsystem;
        private readonly List<GameObject> _spawnedObjects = new();

        [SetUp]
        public void SetUp()
        {
            _systemHolder = new GameObject("Test_TileOcclusionSubSystem");
            _subsystem = _systemHolder.AddComponent<TileOcclusionSubSystem>();
            _spawnedObjects.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(_spawnedObjects[i]);
                }
            }

            _spawnedObjects.Clear();

            if (_systemHolder != null)
            {
                Object.DestroyImmediate(_systemHolder);
            }
        }

        private TileOcclusionMember CreateMember(TileLayer layer, Vector2Int position)
        {
            GameObject go = new($"Test_Member_{layer}_{position}");
            _spawnedObjects.Add(go);
            TileOcclusionMember member = go.AddComponent<TileOcclusionMember>();
            member.SetupForTesting(layer, position);
            return member;
        }

        private void PlaceTurfGridAround(Vector2Int center)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int pos = new(center.x + dx, center.y + dy);
                    TileOcclusionMember turf = CreateMember(TileLayer.Turf, pos);
                    _subsystem.RegisterMember(turf, pos, TileLayer.Turf);
                }
            }
        }

        #region Tests

        [Test]
        public void IsCoverableLayer_ReturnsTrue_ForUnderFloorLayers()
        {
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.Plenum));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.Wire));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.Disposal));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.PipeSurface));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.PipeMiddle));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.PipeLeft));
            Assert.IsTrue(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.PipeRight));
        }

        [Test]
        public void IsCoverableLayer_ReturnsFalse_ForNonUnderFloorLayers()
        {
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.Turf));
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.FurnitureBase));
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.FurnitureTop));
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.Overlays));
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.WallMountHigh));
            Assert.IsFalse(TileOcclusionSubSystem.IsCoverableLayer(TileLayer.WallMountLow));
        }

        [Test]
        public void IsFullyCovered_ReturnsFalse_WhenEmpty()
        {
            Assert.IsFalse(_subsystem.IsFullyCovered(Vector2Int.zero));
        }

        [Test]
        public void IsFullyCovered_ReturnsFalse_WhenOnlyCenterTurfPresent()
        {
            Vector2Int pos = Vector2Int.zero;
            TileOcclusionMember turf = CreateMember(TileLayer.Turf, pos);
            _subsystem.RegisterMember(turf, pos, TileLayer.Turf);

            Assert.IsFalse(_subsystem.IsFullyCovered(pos));
        }

        [Test]
        public void IsFullyCovered_ReturnsFalse_WhenEightNeighborsCoveredButOneMissing()
        {
            Vector2Int center = Vector2Int.zero;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // Skip (1, 1) to test partial coverage
                    if (dx == 1 && dy == 1)
                    {
                        continue;
                    }

                    Vector2Int pos = new(center.x + dx, center.y + dy);
                    TileOcclusionMember turf = CreateMember(TileLayer.Turf, pos);
                    _subsystem.RegisterMember(turf, pos, TileLayer.Turf);
                }
            }

            Assert.IsFalse(_subsystem.IsFullyCovered(center));
        }

        [Test]
        public void IsFullyCovered_ReturnsTrue_WhenAllNinePositionsCovered()
        {
            Vector2Int center = Vector2Int.zero;
            PlaceTurfGridAround(center);

            Assert.IsTrue(_subsystem.IsFullyCovered(center));
        }

        [Test]
        public void RegisterMember_HidesCoverableMember_WhenUnderFloorLayerPlacedUnderTurf()
        {
            Vector2Int center = new(3, 3);
            PlaceTurfGridAround(center);

            TileOcclusionMember pipe = CreateMember(TileLayer.PipeSurface, center);
            _subsystem.RegisterMember(pipe, center, TileLayer.PipeSurface);

            Assert.IsFalse(pipe.IsVisible);
        }

        [Test]
        public void RegisterMember_LeavesMemberVisible_WhenPlacedInOpenSpace()
        {
            Vector2Int center = new(0, 0);

            TileOcclusionMember wire = CreateMember(TileLayer.Wire, center);
            _subsystem.RegisterMember(wire, center, TileLayer.Wire);

            Assert.IsTrue(wire.IsVisible);
        }

        [Test]
        public void TurfPlacement_AfterPipePlacement_HidesPipeWhenNinthTurfPlaced()
        {
            Vector2Int center = new(10, 10);
            TileOcclusionMember pipe = CreateMember(TileLayer.Disposal, center);
            _subsystem.RegisterMember(pipe, center, TileLayer.Disposal);

            Assert.IsTrue(pipe.IsVisible);

            // Place 8 turfs around center (all except (11, 11))
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 1 && dy == 1)
                    {
                        continue;
                    }

                    Vector2Int pos = new(center.x + dx, center.y + dy);
                    TileOcclusionMember turf = CreateMember(TileLayer.Turf, pos);
                    _subsystem.RegisterMember(turf, pos, TileLayer.Turf);
                }
            }

            // Still visible because 1 neighbor turf is missing
            Assert.IsTrue(pipe.IsVisible);

            // Now place the 9th turf at (11, 11)
            Vector2Int lastPos = new(11, 11);
            TileOcclusionMember lastTurf = CreateMember(TileLayer.Turf, lastPos);
            _subsystem.RegisterMember(lastTurf, lastPos, TileLayer.Turf);

            // Now fully covered, pipe should be hidden
            Assert.IsFalse(pipe.IsVisible);
        }

        [Test]
        public void TurfDestruction_RevealsPipeImmediately()
        {
            Vector2Int center = new(0, 0);
            TileOcclusionMember pipe = CreateMember(TileLayer.Plenum, center);
            _subsystem.RegisterMember(pipe, center, TileLayer.Plenum);

            Dictionary<Vector2Int, TileOcclusionMember> turfs = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int pos = new(center.x + dx, center.y + dy);
                    TileOcclusionMember turf = CreateMember(TileLayer.Turf, pos);
                    turfs[pos] = turf;
                    _subsystem.RegisterMember(turf, pos, TileLayer.Turf);
                }
            }

            Assert.IsFalse(pipe.IsVisible);

            // Destroy one neighbor turf at (0, 1)
            Vector2Int targetPos = new(0, 1);
            TileOcclusionMember destroyedTurf = turfs[targetPos];
            _subsystem.UnregisterMember(destroyedTurf, targetPos, TileLayer.Turf);

            // Pipe should now be visible again
            Assert.IsTrue(pipe.IsVisible);
        }

        [Test]
        public void MultipleTurfsOnSameCell_MaintainsCoverageUntilLastTurfRemoved()
        {
            Vector2Int center = new(0, 0);
            PlaceTurfGridAround(center);

            // Add a second turf at center
            TileOcclusionMember secondTurf = CreateMember(TileLayer.Turf, center);
            _subsystem.RegisterMember(secondTurf, center, TileLayer.Turf);

            Assert.AreEqual(2, _subsystem.GetTurfCount(center));

            TileOcclusionMember wire = CreateMember(TileLayer.Wire, center);
            _subsystem.RegisterMember(wire, center, TileLayer.Wire);
            Assert.IsFalse(wire.IsVisible);

            // Remove only one turf at center
            _subsystem.UnregisterMember(secondTurf, center, TileLayer.Turf);
            Assert.AreEqual(1, _subsystem.GetTurfCount(center));
            Assert.IsTrue(_subsystem.IsFullyCovered(center));
            Assert.IsFalse(wire.IsVisible);
        }

        [Test]
        public void DuplicateRegistration_DoesNotCreateDuplicateEntries()
        {
            Vector2Int pos = new(5, 5);
            TileOcclusionMember wire = CreateMember(TileLayer.Wire, pos);

            _subsystem.RegisterMember(wire, pos, TileLayer.Wire);
            _subsystem.RegisterMember(wire, pos, TileLayer.Wire);

            Assert.AreEqual(1, _subsystem.GetCoverableCount(pos));
        }

        #endregion
    }
}
