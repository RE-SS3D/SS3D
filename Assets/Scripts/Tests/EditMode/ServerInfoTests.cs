using NUnit.Framework;
using SS3D.Systems.Rounds;

namespace EditorTests
{
    public class ServerInfoTests
    {
        [Test]
        public void SanitizeName_EmptyFallsBackToDefault()
        {
            Assert.AreEqual(ServerInfoText.DefaultName, ServerInfoText.SanitizeName(null));
            Assert.AreEqual(ServerInfoText.DefaultName, ServerInfoText.SanitizeName(" \t\n "));
        }

        [Test]
        public void SanitizeName_IsSingleLineTrimmedAndClamped()
        {
            Assert.AreEqual("USS Horizon test", ServerInfoText.SanitizeName("  USS\nHorizon \u0007  test "));
            Assert.AreEqual(ServerInfoText.MaxNameLength, ServerInfoText.SanitizeName(new string('x', 200)).Length);
        }

        [Test]
        public void SanitizeMotd_KeepsParagraphsButCollapsesBlankRuns()
        {
            string motd = ServerInfoText.SanitizeMotd("\r\nWelcome   aboard.  \r\n\r\n\r\n\r\nRules:\tbe nice\u0000");
            Assert.AreEqual("Welcome aboard.\n\nRules: be nice", motd);
        }

        [Test]
        public void SanitizeMotd_ClampsLengthAndDefaultsWhenEmpty()
        {
            Assert.AreEqual(ServerInfoText.MaxMotdLength, ServerInfoText.SanitizeMotd(new string('m', 5000)).Length);
            Assert.AreEqual(ServerInfoText.DefaultMotd, ServerInfoText.SanitizeMotd(""));
        }

        [Test]
        public void BuildInfoParse_ReadsGeneratedJson()
        {
            BuildInfo.Data data = BuildInfo.Parse(
                "{\"commit\":\"007c4e9\",\"entries\":[{\"hash\":\"007c4e9\",\"date\":\"2026-10-31\",\"subject\":\"Lobby chat\"}]}");
            Assert.AreEqual("007c4e9", data.commit);
            Assert.AreEqual(1, data.entries.Length);
            Assert.AreEqual("Lobby chat", data.entries[0].subject);
        }

        [Test]
        public void BuildInfoFromCommit_UsesPullRequestTitle()
        {
            BuildInfo.Entry entry = BuildInfo.FromCommit("468f6e0", "2026-10-31",
                "Merge pull request #94 from henkhooft/claude/linux-build-bugs",
                "\nFix bugs found testing Linux client + dedicated server builds\n", "0.1.1");
            Assert.AreEqual(94, entry.pr);
            Assert.AreEqual("Fix bugs found testing Linux client + dedicated server builds", entry.subject);
            Assert.AreEqual("0.1.1", entry.release);
            Assert.AreEqual("https://github.com/o/r/pull/94", entry.Url("https://github.com/o/r"));
        }

        [Test]
        public void BuildInfoFromCommit_KeepsDirectCommitsAndDropsBranchSyncs()
        {
            BuildInfo.Entry direct = BuildInfo.FromCommit("bc84f63", "2026-10-31", "Share emissive handling", "", null);
            Assert.AreEqual(0, direct.pr);
            Assert.AreEqual(string.Empty, direct.release);
            Assert.AreEqual("https://github.com/o/r/commit/bc84f63", direct.Url("https://github.com/o/r"));
            Assert.IsNull(direct.Url(""));
            Assert.IsNull(BuildInfo.FromCommit("db4f6c8", "2026-10-31", "Merge remote-tracking branch 'origin/develop'", "", null));
        }

        [TestCase("git@github.com:henkhooft/SS3D.git", "https://github.com/henkhooft/SS3D")]
        [TestCase("https://github.com/henkhooft/SS3D", "https://github.com/henkhooft/SS3D")]
        [TestCase("https://github.com/henkhooft/SS3D.git/", "https://github.com/henkhooft/SS3D")]
        [TestCase("ssh://git@github.com/henkhooft/SS3D.git", "https://github.com/henkhooft/SS3D")]
        [TestCase("https://gitlab.com/someone/SS3D.git", null)]
        [TestCase("", null)]
        public void BuildInfoRepositoryWebUrl_NormalizesGitHubRemotes(string remote, string expected)
        {
            Assert.AreEqual(expected, BuildInfo.RepositoryWebUrl(remote));
        }

        [Test]
        public void BuildInfoParse_MissingOrBrokenFileIsEmpty()
        {
            Assert.IsEmpty(BuildInfo.Parse(null).entries);
            Assert.IsEmpty(BuildInfo.Parse("not json").entries);
            Assert.IsNull(BuildInfo.Parse("").commit);
        }
    }
}
