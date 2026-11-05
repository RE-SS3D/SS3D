using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Audio;
using SS3D.Systems.Audio.Editor;
using UnityEditor;

namespace EditorTests
{
    public sealed class AmbientLoopSelectionTests
    {
        private readonly HashSet<int> _selected = new();

        private void Select(int maxCount, params AmbientLoopCandidate[] candidates)
        {
            AmbientLoopSelection.SelectNearest(new List<AmbientLoopCandidate>(candidates), maxCount, _selected);
        }

        [Test]
        public void MoreMachinesThanVoices_KeepsTheNearest()
        {
            Select(2,
                new AmbientLoopCandidate(0, 25f, 10f),
                new AmbientLoopCandidate(1, 4f, 10f),
                new AmbientLoopCandidate(2, 9f, 10f));

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, _selected);
        }

        [Test]
        public void MachineBeyondItsReach_NeverTakesAVoice()
        {
            Select(12,
                new AmbientLoopCandidate(0, 36f, 5f),
                new AmbientLoopCandidate(1, 36f, 7f));

            CollectionAssert.AreEquivalent(new[] { 1 }, _selected);
        }

        [Test]
        public void EqualDistances_PickTheSameMachinesWhateverTheOrder()
        {
            Select(1, new AmbientLoopCandidate(1, 4f, 5f), new AmbientLoopCandidate(0, 4f, 5f));
            CollectionAssert.AreEquivalent(new[] { 0 }, _selected);

            Select(1, new AmbientLoopCandidate(0, 4f, 5f), new AmbientLoopCandidate(1, 4f, 5f));
            CollectionAssert.AreEquivalent(new[] { 0 }, _selected);
        }

        [Test]
        public void NothingRunning_ClearsTheLastSelection()
        {
            Select(12, new AmbientLoopCandidate(0, 1f, 5f));
            Select(12);

            Assert.IsEmpty(_selected);
        }
    }

    public sealed class AmbientLoopContentTests
    {
        [Test]
        public void MachinePrefabs_MatchTheRecipeList()
        {
            List<string> drift = AudioContentPrefabRecipes.FindMachineLoopDrift();

            Assert.IsEmpty(drift, string.Join("\n", drift));
        }

        [Test]
        public void EveryKind_PointsAtAnImportedClip()
        {
            foreach (AmbientLoopKind kind in Enum.GetValues(typeof(AmbientLoopKind)))
            {
                AmbientLoopSettings settings = AmbientLoopTrackIds.Get(kind);

                Assert.Greater(settings.MaxDistance, 0f, kind.ToString());
                Assert.IsNotEmpty(AssetDatabase.GUIDToAssetPath(settings.TrackId), kind.ToString());
            }
        }
    }
}
