using NUnit.Framework;
using SS3D.Systems.Examine;

namespace EditorTests
{
    /// <summary>Range bands (examine.md §2): fine print and the flavour line show only up close.</summary>
    public class ExamineDetailTextTests
    {
        private static ExamineContent Content() => new(
            "Gas tank",
            "A blue tank.",
            new[]
            {
                new ExamineSection("Seal: intact."),
                new ExamineSection("Tank: 40% full.", ExamineBand.Close),
            });

        [Test]
        public void CloseShowsDescriptionAndEveryLine()
        {
            Assert.AreEqual(
                "A blue tank.\n\nSeal: intact.\n\nTank: 40% full.",
                ExamineDetailText.Build(Content(), withinCloseRange: true));
        }

        [Test]
        public void FarDropsDescriptionAndCloseLines()
        {
            Assert.AreEqual("Seal: intact.", ExamineDetailText.Build(Content(), withinCloseRange: false));
        }

        [Test]
        public void ExtraSectionsFollowTheSameBands()
        {
            ExamineSection[] extra =
            {
                new("He is dead."),
                new("Fine print.", ExamineBand.Close),
            };

            Assert.AreEqual(
                "Seal: intact.\n\nHe is dead.",
                ExamineDetailText.Build(Content(), withinCloseRange: false, extra));
        }

        [Test]
        public void BlankLinesAreSkipped()
        {
            ExamineContent content = new("X", string.Empty, new[] { new ExamineSection("  "), new ExamineSection("A") });
            Assert.AreEqual("A", ExamineDetailText.Build(content, withinCloseRange: true));
        }
    }
}
