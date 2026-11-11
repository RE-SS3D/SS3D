using NUnit.Framework;
using SS3D.UI.MainHud;
using SS3D.UI.MainHud.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTests
{
    /// <summary>
    /// HUD hand/gear slot drags are left-button only (#214). UI Toolkit sends a down and an up per mouse button, so a
    /// right press used to start a drag, and a second button mid-drag restarted it.
    /// </summary>
    public sealed class HudSlotDragButtonTests
    {
        private EditorWindow _window;
        private HandsGearStrip _strip;
        private VisualElement _slot;
        private Texture2D _texture;
        private int _started;
        private int _ended;

        [SetUp]
        public void SetUp()
        {
            _window = ScriptableObject.CreateInstance<EditorWindow>();
            _window.Show();
            _strip = new HandsGearStrip(default(MainHudIconSet));
            _window.rootVisualElement.Add(_strip);
            _texture = new Texture2D(4, 4);
            _strip.SetHandContents(HandsGearStrip.HandSlot.Left, Sprite.Create(_texture, new Rect(0, 0, 4, 4), Vector2.zero), "Toolbox");
            _slot = _strip.GetHandInventorySlot(HandsGearStrip.HandSlot.Left);
            _strip.HandDragStarted += (_, _) => _started++;
            _strip.HandDragEnded += (_, _) => _ended++;
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void RightButton_DoesNotStartADrag()
        {
            Send(EventType.MouseDown, 1);
            Send(EventType.MouseUp, 1);

            Assert.AreEqual(0, _started);
            Assert.IsFalse(_slot.HasPointerCapture(PointerId.mousePointerId));
        }

        [Test]
        public void SecondButtonMidDrag_NeitherRestartsNorEndsTheDrag()
        {
            Send(EventType.MouseDown, 0);
            Send(EventType.MouseDown, 1);
            Send(EventType.MouseUp, 1);

            Assert.AreEqual(1, _started, "one drag started");
            Assert.AreEqual(0, _ended, "releasing the right button doesn't drop the item");
            Assert.IsTrue(_slot.HasPointerCapture(PointerId.mousePointerId), "the left drag still holds the mouse");

            Send(EventType.MouseUp, 0);

            Assert.AreEqual(1, _ended, "releasing the left button ends the drag");
            Assert.IsFalse(_slot.HasPointerCapture(PointerId.mousePointerId));
        }

        private void Send(EventType type, int button)
        {
            var systemEvent = new Event { type = type, button = button, mousePosition = _slot.worldBound.center, clickCount = 1 };
            EventBase evt = type == EventType.MouseDown
                ? PointerDownEvent.GetPooled(systemEvent)
                : PointerUpEvent.GetPooled(systemEvent);
            using (evt)
            {
                evt.target = _slot;
                _slot.SendEvent(evt);
            }
        }
    }
}
