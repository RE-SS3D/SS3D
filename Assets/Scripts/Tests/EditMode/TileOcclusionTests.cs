using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Behavioral tests: visibility is asserted on the real <see cref="Renderer.enabled"/> flag of real
    /// MeshRenderers (root and child), never on an internal flag. No test-only members exist in production code.
    /// </summary>
    public class TileOcclusionTests
    {
        private readonly List<GameObject> _spawnedObjects = new();
        private GameObject _systemHolder;
        private TileOcclusionSubSystem _subsystem;

        [SetUp]
        public void SetUp()
        {
            _systemHolder = new GameObject("Test_TileOcclusionSubSystem");
            _subsystem = _systemHolder.AddComponent<TileOcclusionSubSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawned in _spawnedObjects)
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned);
                }
            }

            _spawnedObjects.Clear();
            Object.DestroyImmediate(_systemHolder);
        }

        [Test]
        public void SetVisible_False_DisablesRootAndChildRenderers_AndTrueRestoresThem()
        {
            TileOcclusionMember member = CreateMember(out Renderer[] renderers);

            member.SetVisible(false);
            Assert.IsTrue(AllRenderers(renderers, false), "every renderer, including the child, must be disabled");

            member.SetVisible(true);
            Assert.IsTrue(AllRenderers(renderers, true), "every renderer must be enabled again");
        }

        [Test]
        public void IsFullyCovered_IsFalse_WhenNothingIsPlaced()
        {
            Assert.IsFalse(_subsystem.IsFullyCovered(Vector2Int.zero));
        }

        [Test]
        public void IsFullyCovered_IsFalse_WhenOnlyCenterTurfExists()
        {
            PlaceTurf(Vector2Int.zero);

            Assert.IsFalse(_subsystem.IsFullyCovered(Vector2Int.zero));
        }

        [Test]
        public void IsFullyCovered_IsTrue_OnlyWhenAllNineCellsHaveTurf()
        {
            PlaceTurfBlock(Vector2Int.zero, null);

            Assert.IsTrue(_subsystem.IsFullyCovered(Vector2Int.zero));
        }

        [Test]
        public void IsFullyCovered_IsFalse_WhenAnySingleCellOfTheBlockIsMissing([Range(-1, 1)] int missingDx, [Range(-1, 1)] int missingDy)
        {
            Vector2Int center = new(7, -4);
            PlaceTurfBlock(center, new Vector2Int(center.x + missingDx, center.y + missingDy));

            Assert.IsFalse(_subsystem.IsFullyCovered(center));
        }

        [TestCase(TileLayer.Plenum, true)]
        [TestCase(TileLayer.Wire, true)]
        [TestCase(TileLayer.Disposal, true)]
        [TestCase(TileLayer.PipeSurface, true)]
        [TestCase(TileLayer.PipeMiddle, true)]
        [TestCase(TileLayer.PipeLeft, true)]
        [TestCase(TileLayer.PipeRight, true)]
        [TestCase(TileLayer.Turf, false)]
        [TestCase(TileLayer.WallMountHigh, false)]
        [TestCase(TileLayer.WallMountLow, false)]
        [TestCase(TileLayer.FurnitureBase, false)]
        [TestCase(TileLayer.FurnitureTop, false)]
        [TestCase(TileLayer.Overlays, false)]
        public void ObjectUnderFullTurfCoverage_IsHiddenOnlyIfItsLayerIsCoverable(TileLayer layer, bool expectedHidden)
        {
            Vector2Int cell = new(2, 2);
            PlaceTurfBlock(cell, null);

            TileOcclusionMember member = CreateMember(out Renderer[] renderers);
            _subsystem.RegisterMember(member, cell, layer);

            Assert.AreEqual(!expectedHidden, AllRenderers(renderers, true));
        }

        [Test]
        public void ObjectPlacedInOpenSpace_StaysVisible()
        {
            TileOcclusionMember wire = CreateMember(out Renderer[] renderers);

            _subsystem.RegisterMember(wire, Vector2Int.zero, TileLayer.Wire);

            Assert.IsTrue(AllRenderers(renderers, true));
        }

        [Test]
        public void PlacingTheNinthTurf_HidesAnAlreadyPlacedPipe()
        {
            Vector2Int cell = new(10, 10);
            Vector2Int lastTurfCell = new(11, 11);
            TileOcclusionMember pipe = CreateMember(out Renderer[] renderers);
            _subsystem.RegisterMember(pipe, cell, TileLayer.Disposal);

            PlaceTurfBlock(cell, lastTurfCell);
            Assert.IsTrue(AllRenderers(renderers, true), "eight of nine turfs must not hide the pipe");

            PlaceTurf(lastTurfCell);
            Assert.IsTrue(AllRenderers(renderers, false), "the ninth turf must hide the pipe");
        }

        [Test]
        public void RemovingOneSurroundingTurf_RevealsThePipeAgain()
        {
            Vector2Int cell = Vector2Int.zero;
            Dictionary<Vector2Int, TileOcclusionMember> turfs = PlaceTurfBlock(cell, null);
            TileOcclusionMember pipe = CreateMember(out Renderer[] renderers);
            _subsystem.RegisterMember(pipe, cell, TileLayer.Plenum);
            Assert.IsTrue(AllRenderers(renderers, false), "precondition: pipe starts hidden");

            Vector2Int removed = new(0, 1);
            _subsystem.UnregisterMember(turfs[removed], removed, TileLayer.Turf);

            Assert.IsTrue(AllRenderers(renderers, true));
        }

        [Test]
        public void RemovingATurf_OnlyRevealsObjectsWithinOneCell()
        {
            Dictionary<Vector2Int, TileOcclusionMember> turfs = PlaceTurfBlock(new Vector2Int(0, 0), null);
            PlaceTurfBlock(new Vector2Int(10, 0), null);
            TileOcclusionMember near = CreateMember(out Renderer[] nearRenderers);
            TileOcclusionMember far = CreateMember(out Renderer[] farRenderers);
            _subsystem.RegisterMember(near, new Vector2Int(0, 0), TileLayer.Wire);
            _subsystem.RegisterMember(far, new Vector2Int(10, 0), TileLayer.Wire);

            _subsystem.UnregisterMember(turfs[new Vector2Int(1, 0)], new Vector2Int(1, 0), TileLayer.Turf);

            Assert.IsTrue(AllRenderers(nearRenderers, true));
            Assert.IsTrue(AllRenderers(farRenderers, false), "a distant covered object must be unaffected");
        }

        [Test]
        public void TwoTurfsOnTheSameCell_KeepCoverageUntilTheLastOneIsRemoved()
        {
            Vector2Int cell = Vector2Int.zero;
            PlaceTurfBlock(cell, null);
            TileOcclusionMember secondTurf = PlaceTurf(cell);
            TileOcclusionMember wire = CreateMember(out Renderer[] renderers);
            _subsystem.RegisterMember(wire, cell, TileLayer.Wire);

            _subsystem.UnregisterMember(secondTurf, cell, TileLayer.Turf);

            Assert.IsTrue(_subsystem.IsFullyCovered(cell));
            Assert.IsTrue(AllRenderers(renderers, false), "the original turf still covers the wire");
        }

        [Test]
        public void RegisteringTheSameTurfTwice_DoesNotLeavePhantomCoverageAfterItIsRemoved()
        {
            Vector2Int cell = Vector2Int.zero;
            Dictionary<Vector2Int, TileOcclusionMember> turfs = PlaceTurfBlock(cell, null);
            _subsystem.RegisterMember(turfs[cell], cell, TileLayer.Turf);

            _subsystem.UnregisterMember(turfs[cell], cell, TileLayer.Turf);

            Assert.IsFalse(_subsystem.IsFullyCovered(cell), "one removal must remove the only turf on the cell");
        }

        [Test]
        public void UnregisteredCoverableMember_IsNoLongerUpdated()
        {
            Vector2Int cell = Vector2Int.zero;
            TileOcclusionMember wire = CreateMember(out Renderer[] renderers);
            _subsystem.RegisterMember(wire, cell, TileLayer.Wire);
            _subsystem.RegisterMember(wire, cell, TileLayer.Wire);

            _subsystem.UnregisterMember(wire, cell, TileLayer.Wire);
            PlaceTurfBlock(cell, null);

            Assert.IsTrue(AllRenderers(renderers, true), "a single unregister must drop a doubly-registered member");
        }

        [Test]
        public void DestroyedMember_DoesNotBreakUpdatesOfItsNeighbors()
        {
            Vector2Int cell = Vector2Int.zero;
            TileOcclusionMember destroyed = CreateMember(out _);
            TileOcclusionMember survivor = CreateMember(out Renderer[] survivorRenderers);
            _subsystem.RegisterMember(destroyed, cell, TileLayer.Wire);
            _subsystem.RegisterMember(survivor, cell, TileLayer.Disposal);
            Object.DestroyImmediate(destroyed.gameObject);

            Assert.DoesNotThrow(() => PlaceTurfBlock(cell, null));
            Assert.IsTrue(AllRenderers(survivorRenderers, false), "the surviving object must still be hidden");
        }

        [Test]
        public void UnregisteringAMemberThatWasNeverRegistered_IsIgnored()
        {
            TileOcclusionMember stranger = CreateMember(out _);

            Assert.DoesNotThrow(() => _subsystem.UnregisterMember(stranger, Vector2Int.zero, TileLayer.Turf));
            Assert.DoesNotThrow(() => _subsystem.UnregisterMember(stranger, Vector2Int.zero, TileLayer.Wire));
            Assert.IsFalse(_subsystem.IsFullyCovered(Vector2Int.zero));
        }

        private static bool AllRenderers(Renderer[] renderers, bool enabled)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled != enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private TileOcclusionMember CreateMember(out Renderer[] renderers)
        {
            GameObject root = new("Test_Member");
            GameObject child = new("Child");
            child.transform.SetParent(root.transform);
            _spawnedObjects.Add(root);

            renderers = new Renderer[]
            {
                AddMeshRenderer(root),
                AddMeshRenderer(child),
            };

            return root.AddComponent<TileOcclusionMember>();
        }

        private static Renderer AddMeshRenderer(GameObject target)
        {
            target.AddComponent<MeshFilter>();
            return target.AddComponent<MeshRenderer>();
        }

        private TileOcclusionMember PlaceTurf(Vector2Int cell)
        {
            TileOcclusionMember turf = CreateMember(out _);
            _subsystem.RegisterMember(turf, cell, TileLayer.Turf);
            return turf;
        }

        private Dictionary<Vector2Int, TileOcclusionMember> PlaceTurfBlock(Vector2Int center, Vector2Int? skip)
        {
            Dictionary<Vector2Int, TileOcclusionMember> turfs = new();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int cell = new(center.x + dx, center.y + dy);

                    if (skip.HasValue && skip.Value == cell)
                    {
                        continue;
                    }

                    turfs[cell] = PlaceTurf(cell);
                }
            }

            return turfs;
        }
    }
}
