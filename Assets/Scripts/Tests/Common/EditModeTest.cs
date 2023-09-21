using NUnit.Framework;

namespace SS3D.Tests
{
    /// <summary>
    /// Base for edit mode tests
    /// </summary>
    public abstract class EditModeTest: Test
    {
        [SetUp]
        public override void SetUp() => base.SetUp();

        [TearDown]
        public override void TearDown() => base.TearDown();
    }
}