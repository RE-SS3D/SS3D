using NUnit.Framework;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P1.4: the server, not the client, decides who drags furniture and from how far.</summary>
    public sealed class DraggableRulesTests
    {
        private GameObject _caller;
        private GameObject _other;

        [SetUp]
        public void SetUp()
        {
            _caller = new GameObject("caller");
            _other = new GameObject("other");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_caller);
            Object.DestroyImmediate(_other);
        }

        [Test]
        public void Start_InReach_IsAllowed()
        {
            Assert.IsTrue(Draggable.MayChangeDrag(true, _caller.transform, null, new Vector3(2f, 5f, 0f)));
        }

        [Test]
        public void Start_OutOfReach_IsRefused()
        {
            Vector3 far = new(Draggable.MaxDragStartDistance + 0.5f, 0f, 0f);
            Assert.IsFalse(Draggable.MayChangeDrag(true, _caller.transform, null, far));
        }

        [Test]
        public void Start_WhileSomeoneElseDrags_IsRefused()
        {
            Assert.IsFalse(Draggable.MayChangeDrag(true, _caller.transform, _other.transform, Vector3.zero));
        }

        [Test]
        public void Release_OnlyByTheDragger()
        {
            Assert.IsTrue(Draggable.MayChangeDrag(false, _caller.transform, _caller.transform, Vector3.one * 50f));
            Assert.IsFalse(Draggable.MayChangeDrag(false, _caller.transform, _other.transform, Vector3.zero));
            Assert.IsTrue(Draggable.MayChangeDrag(false, _caller.transform, null, Vector3.zero));
        }

        [Test]
        public void NoCallerBody_IsRefused()
        {
            Assert.IsFalse(Draggable.MayChangeDrag(true, null, null, Vector3.zero));
        }
    }
}
