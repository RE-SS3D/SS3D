using NUnit.Framework;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Q3 of Documents/architecture/2026-11_map-editor-systems-views.md: a network keeps its view id (its colour)
    /// through rebuilds; the side sharing most segments keeps it, ties go to the lower id, others get fresh ids.
    /// </summary>
    public sealed class NetworkIdentityTrackerTests
    {
        private static int[] Assign(NetworkIdentityTracker<int> tracker, params int[][] networks) =>
            tracker.Assign(networks.Select(network => (IEnumerable<int>)network).ToList());

        private static int[] Range(int from, int count) => Enumerable.Range(from, count).ToArray();

        [Test]
        public void FirstBuild_NumbersFromOne()
        {
            NetworkIdentityTracker<int> tracker = new();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Assign(tracker, Range(0, 3), Range(10, 2), Range(20, 1)));
            Assert.IsTrue(tracker.TryGetViewId(11, out int id));
            Assert.AreEqual(2, id);
        }

        [Test]
        public void Merge_TheLargerPartKeepsItsId()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 3), Range(10, 5));

            // A joining pipe at 99 links both networks.
            int[] merged = Assign(tracker, Range(0, 3).Concat(Range(10, 5)).Append(99).ToArray());

            CollectionAssert.AreEqual(new[] { 2 }, merged);
        }

        [Test]
        public void Split_TheLargerSideKeepsItsId_TheOtherGetsAFreshOne()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 6), Range(50, 2));

            // Removing segment 2 splits network 1 into {0,1} and {3,4,5}.
            int[] split = Assign(tracker, Range(0, 2), Range(3, 3), Range(50, 2));

            CollectionAssert.AreEqual(new[] { 3, 1, 2 }, split);
        }

        [Test]
        public void FullRecreate_WithTheSameSegments_KeepsEveryId()
        {
            NetworkIdentityTracker<int> tracker = new();
            int[] before = Assign(tracker, Range(0, 4), Range(10, 3), Range(20, 6));

            // New records, new collections, another order: identity follows the segments.
            int[] after = Assign(tracker, Range(20, 6).Reverse().ToArray(), Range(0, 4), Range(10, 3));

            CollectionAssert.AreEqual(new[] { before[2], before[0], before[1] }, after);
        }

        [Test]
        public void Tie_GoesToTheLowerId()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 2), Range(10, 2));

            CollectionAssert.AreEqual(new[] { 1 }, Assign(tracker, Range(0, 2).Concat(Range(10, 2)).ToArray()));
        }

        [Test]
        public void EvenSplit_TheEarlierNetworkKeepsTheId()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 4));

            CollectionAssert.AreEqual(new[] { 1, 2 }, Assign(tracker, Range(2, 2), Range(0, 2)));
        }

        [Test]
        public void NewNetwork_NeverReusesADeletedNetworksId()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 2), Range(10, 2), Range(20, 2));

            // Network 2 was removed; a brand new one gets a fresh id, not its colour and label.
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, Assign(tracker, Range(0, 2), Range(20, 2), Range(30, 1)));
            CollectionAssert.AreEqual(new[] { 1, 5 }, Assign(tracker, Range(0, 2), Range(40, 1)));
        }

        [Test]
        public void Clear_ForgetsEveryNetwork()
        {
            NetworkIdentityTracker<int> tracker = new();
            Assign(tracker, Range(0, 2), Range(10, 2));
            tracker.Clear();

            CollectionAssert.AreEqual(new[] { 1 }, Assign(tracker, Range(10, 2)));
            Assert.IsFalse(tracker.TryGetViewId(0, out _));
        }
    }
}
