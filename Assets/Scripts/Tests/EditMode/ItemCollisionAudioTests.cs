using System;
using System.IO;
using NUnit.Framework;
using SS3D.Systems.Audio;
using SS3D.Systems.Audio.Editor;

namespace EditorTests
{
    public sealed class CollisionSoundLimiterTests
    {
        [Test]
        public void Source_IsQuietUntilItsCooldownEnds()
        {
            var limiter = new CollisionSoundLimiter();
            float ready = 0f;

            Assert.IsTrue(limiter.TryPlay(10f, ref ready));
            Assert.IsFalse(limiter.TryPlay(10f + CollisionSoundLimiter.SourceCooldown * 0.5f, ref ready));
            Assert.IsTrue(limiter.TryPlay(10f + CollisionSoundLimiter.SourceCooldown, ref ready));
        }

        [Test]
        public void ManySources_AtOnce_StopAtTheBurst()
        {
            var limiter = new CollisionSoundLimiter();
            int played = 0;
            for (int i = 0; i < 50; i++)
            {
                float ready = 0f;
                if (limiter.TryPlay(10f, ref ready))
                {
                    played++;
                }
            }

            Assert.AreEqual(CollisionSoundLimiter.Burst, played);
        }

        [Test]
        public void ManySources_OverTime_HoldToTheRefillRate()
        {
            var limiter = new CollisionSoundLimiter();
            const float Seconds = 20f;
            const float Step = 1f / 60f;
            int played = 0;
            for (float now = 10f; now < 10f + Seconds; now += Step)
            {
                for (int i = 0; i < 10; i++)
                {
                    float ready = 0f;
                    if (limiter.TryPlay(now, ref ready))
                    {
                        played++;
                    }
                }
            }

            float expected = CollisionSoundLimiter.Burst + Seconds * CollisionSoundLimiter.RefillPerSecond;
            Assert.That(played, Is.InRange(expected * 0.9f, expected * 1.05f));
        }

        [Test]
        public void SourceOnCooldown_DoesNotSpendBudget()
        {
            var limiter = new CollisionSoundLimiter();
            float bouncing = 0f;
            limiter.TryPlay(10f, ref bouncing);
            for (int i = 0; i < 100; i++)
            {
                limiter.TryPlay(10f, ref bouncing);
            }

            float other = 0f;
            Assert.IsTrue(limiter.TryPlay(10f, ref other));
        }
    }

    public sealed class ItemCollisionAudioTests
    {
        [Test]
        public void VolumeForSpeed_RisesWithSpeed_AndHoldsAtTheTop()
        {
            float slow = NoisyCollision.VolumeForSpeed(NoisyCollision.MinImpactSpeed);
            float mid = NoisyCollision.VolumeForSpeed(4f);
            float full = NoisyCollision.VolumeForSpeed(NoisyCollision.FullImpactSpeed);

            Assert.Less(slow, mid);
            Assert.Less(mid, full);
            Assert.AreEqual(full, NoisyCollision.VolumeForSpeed(50f));
        }

        [Test]
        public void EveryMaterial_HasRegisteredClips()
        {
            string sounds = File.ReadAllText("Assets/Content/Data/Databases/Sounds.asset");
            foreach (ItemCollisionMaterial material in Enum.GetValues(typeof(ItemCollisionMaterial)))
            {
                string[] clips = ItemCollisionAudioTrackIds.ClipsFor(material);
                Assert.IsNotEmpty(clips, material.ToString());
                foreach (string clip in clips)
                {
                    StringAssert.Contains($"_key: {clip}", sounds, $"{material} clip isn't in Sounds.asset");
                }
            }
        }

        [Test]
        public void ItemPrefabs_MatchTheRecipeLists()
        {
            Assert.IsEmpty(AudioContentPrefabRecipes.FindDrift(), string.Join("\n", AudioContentPrefabRecipes.FindDrift()));
        }
    }
}
