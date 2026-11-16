using NUnit.Framework;
using SS3D.Core.Debugging;

namespace EditorTests
{
    public class DebugToolsTests
    {
        [TearDown]
        public void TearDown() => DebugTools.Clear();

        [Test]
        public void SameNameIsOneTool()
        {
            Assert.That(DebugTools.Get("Health"), Is.SameAs(DebugTools.Get("Health")));
            Assert.That(DebugTools.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvokingAToolReachesEverySubscriber()
        {
            int calls = 0;
            DebugTools.Get("See Container Contents").Invoked += () => calls++;
            DebugTools.Get("See Container Contents").Invoked += () => calls++;

            DebugTools.Get("See Container Contents").Invoke();

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void ToolWithoutSubscribersIsUnavailable()
        {
            DebugTool tool = DebugTools.Get("Knock Down");
            void Handler() { }

            Assert.That(tool.Available, Is.False);
            tool.Invoked += Handler;
            Assert.That(tool.Available, Is.True);
            tool.Invoked -= Handler;
            Assert.That(tool.Available, Is.False);
        }

        [Test]
        public void ToggleToolReportsTheOwnersState()
        {
            bool open = false;
            DebugTool tool = DebugTools.Get("Atmospherics");
            tool.Invoked += () => open = !open;
            tool.IsOn = () => open;

            tool.Invoke();

            Assert.That(tool.IsToggle, Is.True);
            Assert.That(tool.IsOn(), Is.True);
            Assert.That(DebugTools.Get("Local Speech Line").IsToggle, Is.False);
        }
    }
}
