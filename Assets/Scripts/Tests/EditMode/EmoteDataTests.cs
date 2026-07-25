using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace EditorTests
{
    public class EmoteDataTests
    {
        private EmoteData _emote;

        [SetUp]
        public void SetUp()
        {
            _emote = ScriptableObject.CreateInstance<EmoteData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_emote);
        }

        [Test]
        public void IsAllowedInPostureReturnsTrueWhenNoPosturesAreDisallowed()
        {
            _emote.DisallowedPostures = new List<Posture>();

            Assert.IsTrue(_emote.IsAllowedInPosture(Posture.Standing));
            Assert.IsTrue(_emote.IsAllowedInPosture(Posture.Sitting));
            Assert.IsTrue(_emote.IsAllowedInPosture(Posture.Prone));
        }

        [Test]
        public void IsAllowedInPostureReturnsFalseForADisallowedPosture()
        {
            _emote.DisallowedPostures = new List<Posture> { Posture.Prone };

            Assert.IsFalse(_emote.IsAllowedInPosture(Posture.Prone));
        }

        [Test]
        public void IsAllowedInPostureReturnsTrueForAPostureNotInTheDisallowedList()
        {
            _emote.DisallowedPostures = new List<Posture> { Posture.Prone };

            Assert.IsTrue(_emote.IsAllowedInPosture(Posture.Standing));
            Assert.IsTrue(_emote.IsAllowedInPosture(Posture.Sitting));
        }
    }
}
