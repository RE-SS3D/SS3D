using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Examine;

namespace EditorTests
{
    public class ExamineKeywordFormatterTests
    {
        private const string TokensPath = "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss";

        [Test]
        public void KnownTagsBecomeColourSpans()
        {
            Assert.AreEqual(
                "Seal: <color=#b94848>breached</color>.",
                ExamineKeywordFormatter.Format("Seal: <k=danger>breached</k>."));
            Assert.AreEqual(
                "<color=#4a8f5c>powered</color>, <color=#c9a227>panel open</color>",
                ExamineKeywordFormatter.Format("<k=good>powered</k>, <k=warn>panel open</k>"));
        }

        [Test]
        public void UnknownTagRendersPlain()
        {
            Assert.AreEqual("It is shiny.", ExamineKeywordFormatter.Format("It is <k=sparkle>shiny</k>."));
        }

        [Test]
        public void DepartmentTagWithoutPaletteRendersPlain()
        {
            Assert.AreEqual("Engineer", ExamineKeywordFormatter.Format("<k=dept:engineering>Engineer</k>"));
            Assert.IsTrue(ExamineKeywordFormatter.IsKnownTag("dept:engineering"));
            Assert.IsFalse(ExamineKeywordFormatter.IsKnownTag("dept:"));
        }

        [Test]
        public void NestedTagsCloseInOrder()
        {
            Assert.AreEqual(
                "<color=#c9a227>low and <color=#b94848>leaking</color></color>",
                ExamineKeywordFormatter.Format("<k=warn>low and <k=danger>leaking</k></k>"));
            Assert.AreEqual(
                "a <color=#b94848>b</color> c",
                ExamineKeywordFormatter.Format("a <k=unknown><k=danger>b</k></k> c"));
        }

        [Test]
        public void MalformedMarkupNeverLeaksTags()
        {
            Assert.AreEqual("<color=#b94848>dead</color>", ExamineKeywordFormatter.Format("<k=danger>dead"));
            Assert.AreEqual("dead", ExamineKeywordFormatter.Format("dead</k>"));
            Assert.AreEqual("broken <k=danger", ExamineKeywordFormatter.Format("broken <k=danger"));
        }

        [Test]
        public void OtherRichTextAndPlainTextPassThrough()
        {
            Assert.AreEqual("<b>bold</b> 5 < 6", ExamineKeywordFormatter.Format("<b>bold</b> 5 < 6"));
            Assert.AreEqual(string.Empty, ExamineKeywordFormatter.Format(null));
        }

        [Test]
        public void StripTagsLeavesWordsOnly()
        {
            Assert.AreEqual(
                "He seems to be hurt and bleeding from the head.",
                ExamineKeywordFormatter.StripTags(
                    "He seems to be <k=warn>hurt</k> and <k=danger>bleeding</k> from the head."));
        }

        [Test]
        public void EnumerateTagsListsEveryTag()
        {
            CollectionAssert.AreEqual(
                new[] { "warn", "danger" },
                ExamineKeywordFormatter.EnumerateTags("<k=warn>a</k> <k=danger>b</k>").ToArray());
        }

        /// <summary>Rich text cannot read USS variables, so the formatter copies the hex values.</summary>
        [Test]
        public void ColoursMatchFeedbackTokens()
        {
            string tokens = File.ReadAllText(TokensPath);
            StringAssert.Contains($"--ss3d-feedback-success: {ExamineKeywordFormatter.GoodColor};", tokens);
            StringAssert.Contains($"--ss3d-feedback-warning: {ExamineKeywordFormatter.WarnColor};", tokens);
            StringAssert.Contains($"--ss3d-feedback-danger: {ExamineKeywordFormatter.DangerColor};", tokens);
        }
    }
}
