using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Audio;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The pooled audio source purge must survive its own removals: it used to remove from the list it was iterating,
    /// which threw and killed the purge coroutine for the rest of the session.
    /// </summary>
    public class AudioSourcePurgeTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Purge_IdleSourcesAboveMinimum_TrimsToMinimumWithoutThrowing()
        {
            List<AudioSource> sources = CreateSources(12);
            List<AudioSource> before = new(sources);

            int removed = 0;
            Assert.DoesNotThrow(() => removed = AudioSubSystem.PurgeIdleAudioSources(sources, 4));

            Assert.AreEqual(8, removed);
            Assert.AreEqual(4, sources.Count);
            Assert.AreEqual(8, before.FindAll(source => source == null).Count, "removed sources are destroyed");
            Assert.IsTrue(sources.TrueForAll(source => source != null), "kept sources are alive");
        }

        [Test]
        public void Purge_DestroyedSources_AreDroppedEvenAtMinimum()
        {
            List<AudioSource> sources = CreateSources(3);
            Object.DestroyImmediate(sources[1].gameObject);

            Assert.DoesNotThrow(() => AudioSubSystem.PurgeIdleAudioSources(sources, 3));

            Assert.AreEqual(2, sources.Count);
            Assert.IsTrue(sources.TrueForAll(source => source != null));
        }

        [Test]
        public void Purge_AtMinimum_KeepsIdleSources()
        {
            List<AudioSource> sources = CreateSources(4);

            Assert.AreEqual(0, AudioSubSystem.PurgeIdleAudioSources(sources, 4));
            Assert.AreEqual(4, sources.Count);
        }

        private List<AudioSource> CreateSources(int count)
        {
            List<AudioSource> sources = new();
            for (int i = 0; i < count; i++)
            {
                GameObject go = new($"PurgeTestSource{i}");
                _created.Add(go);
                sources.Add(go.AddComponent<AudioSource>());
            }

            return sources;
        }
    }
}
