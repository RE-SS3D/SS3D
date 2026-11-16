using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P2.3: registry lifecycle bugs (duplicate unregister, destroyed instances, stale quit flag).</summary>
    public sealed class SubSystemsRegistryTests
    {
        private sealed class PlainSubSystem : ISubSystem
        {
        }

        private sealed class UnitySubSystem : MonoBehaviour, ISubSystem
        {
        }

        private static readonly FieldInfo QuittingField =
            typeof(SubSystems).GetField("_isQuitting", BindingFlags.NonPublic | BindingFlags.Static);

        [Test]
        public void DestroyedDuplicate_DoesNotEvictTheLiveInstance()
        {
            PlainSubSystem live = new();
            PlainSubSystem duplicate = new();
            SubSystems.Register(live);
            SubSystems.Register(duplicate); // ignored: live is already registered
            try
            {
                SubSystems.Unregister(duplicate);
                Assert.IsTrue(SubSystems.TryGet(out PlainSubSystem found));
                Assert.AreSame(live, found);
            }
            finally
            {
                SubSystems.Unregister(live);
            }

            Assert.IsFalse(SubSystems.TryGet(out PlainSubSystem _));
        }

        [Test]
        public void DestroyedInstance_CountsAsMissing()
        {
            GameObject host = new("UnitySubSystem");
            UnitySubSystem subsystem = host.AddComponent<UnitySubSystem>();
            SubSystems.Register(subsystem);
            Object.DestroyImmediate(host);

            Assert.IsFalse(SubSystems.TryGet(out UnitySubSystem _));
            Assert.IsNull(SubSystems.GetOrNull<UnitySubSystem>());
        }

        [Test]
        public void GetOrNull_MissesSilently()
        {
            // No LogAssert.Expect: GetOrNull must not log the error Get does, nor scan the scene.
            Assert.IsNull(SubSystems.GetOrNull<PlainSubSystem>());
        }

        [Test]
        public void NewSession_ClearsTheQuitFlag()
        {
            bool before = (bool)QuittingField.GetValue(null);
            var registry = (System.Collections.Generic.Dictionary<System.Type, ISubSystem>)typeof(SubSystems)
                .GetField("RegisteredSubsystems", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var saved = new System.Collections.Generic.Dictionary<System.Type, ISubSystem>(registry);
            try
            {
                QuittingField.SetValue(null, true);
                typeof(SubSystems).GetMethod("ResetForNewSession", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                Assert.IsFalse((bool)QuittingField.GetValue(null));
            }
            finally
            {
                QuittingField.SetValue(null, before);
                foreach (var pair in saved)
                {
                    registry[pair.Key] = pair.Value;
                }
            }
        }
    }
}
