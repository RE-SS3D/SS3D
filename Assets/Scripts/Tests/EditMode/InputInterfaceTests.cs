using NUnit.Framework;
using SS3D.Systems.Inputs;

namespace EditorTests
{
    /// <summary>
    /// Guards the null-safety and empty-state behaviour of the unified pointer query. Panel picking
    /// itself needs a live runtime panel and is covered by manual/PlayMode verification.
    /// </summary>
    public class InputInterfaceTests
    {
        [Test]
        public void RegisterNullDocumentIsIgnored()
        {
            Assert.DoesNotThrow(() => InputInterface.RegisterDocument(null));
            Assert.DoesNotThrow(() => InputInterface.UnregisterDocument(null));
        }

        [Test]
        public void PointerIsNotOverInterfaceWithNoUi()
        {
            // No EventSystem and no registered documents in an isolated EditMode run.
            Assert.IsFalse(InputInterface.IsPointerOverInterface());
        }

        [Test]
        public void TextCaptureMakesPointerOverInterfaceTrue()
        {
            Assert.IsFalse(InputInterface.IsCapturingText);

            InputInterface.PushTextCapture();
            try
            {
                Assert.IsTrue(InputInterface.IsCapturingText);
                Assert.IsTrue(InputInterface.IsPointerOverInterface());
            }
            finally
            {
                InputInterface.PopTextCapture();
            }

            Assert.IsFalse(InputInterface.IsCapturingText);
            Assert.IsFalse(InputInterface.IsPointerOverInterface());
        }
    }
}
