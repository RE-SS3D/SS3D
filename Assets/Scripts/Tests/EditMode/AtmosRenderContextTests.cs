using NUnit.Framework;
using SS3D.Rendering.URP;

namespace EditorTests
{
    /// <summary>
    /// The map editor suppresses the atmos fullscreen passes and wind streaks without clearing
    /// the simulation (<see cref="AtmosRenderContext.IsRenderingSuppressed"/>).
    /// </summary>
    public class AtmosRenderContextTests
    {
        [TearDown]
        public void ResetSuppression()
        {
            AtmosRenderContext.SetRenderingSuppressed(false);
        }

        [Test]
        public void RenderingIsNotSuppressedByDefault()
        {
            AtmosRenderContext.SetRenderingSuppressed(false);

            Assert.IsFalse(AtmosRenderContext.IsRenderingSuppressed);
        }

        [Test]
        public void SuppressionRoundTrips()
        {
            AtmosRenderContext.SetRenderingSuppressed(true);
            Assert.IsTrue(AtmosRenderContext.IsRenderingSuppressed);

            AtmosRenderContext.SetRenderingSuppressed(false);
            Assert.IsFalse(AtmosRenderContext.IsRenderingSuppressed);
        }
    }
}
