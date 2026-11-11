using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P6.4: components list themselves instead of being found by a scene scan.</summary>
    public sealed class SceneRegistryTests
    {
        private sealed class Member
        {
        }

        [SetUp]
        [TearDown]
        public void Reset() =>
            typeof(SceneRegistry).GetMethod("ResetForNewSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

        [Test]
        public void Register_IsIdempotent_AndUnregisterRemoves()
        {
            var a = new Member();
            var b = new Member();

            SceneRegistry<Member>.Register(a);
            SceneRegistry<Member>.Register(a);
            SceneRegistry<Member>.Register(b);
            Assert.AreEqual(2, SceneRegistry<Member>.Count);

            SceneRegistry<Member>.Unregister(a);
            CollectionAssert.AreEqual(new[] { b }, SceneRegistry<Member>.All);
        }

        [Test]
        public void Null_IsNeverRegistered()
        {
            SceneRegistry<Member>.Register(null);
            Assert.AreEqual(0, SceneRegistry<Member>.Count);
        }

        [Test]
        public void CopyTo_LetsALoopRemoveMembers()
        {
            SceneRegistry<Member>.Register(new Member());
            SceneRegistry<Member>.Register(new Member());
            var buffer = new List<Member> { new() };

            SceneRegistry<Member>.CopyTo(buffer);
            foreach (Member member in buffer)
            {
                SceneRegistry<Member>.Unregister(member);
            }

            Assert.AreEqual(2, buffer.Count, "CopyTo replaces the buffer's contents");
            Assert.AreEqual(0, SceneRegistry<Member>.Count);
        }

        [Test]
        public void NewPlaySession_ClearsEveryRegistry()
        {
            SceneRegistry<Member>.Register(new Member());
            SceneRegistry<string>.Register("leftover");

            Reset();

            Assert.AreEqual(0, SceneRegistry<Member>.Count);
            Assert.AreEqual(0, SceneRegistry<string>.Count);
        }
    }
}
