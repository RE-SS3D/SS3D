using NUnit.Framework;
using SS3D.Systems.Rounds;

namespace EditorTests
{
    public class GameVersionTests
    {
        [Test]
        public void OnReleaseTag_IsTheTagVersion()
        {
            Assert.AreEqual("0.1.0", GameVersion.FromDescribe("v0.1.0-0-g5da58b9", dirty: false));
        }

        [Test]
        public void OnReleaseTagWithLocalChanges_IsMarkedDirty()
        {
            Assert.AreEqual("0.1.0+5da58b9.dirty", GameVersion.FromDescribe("v0.1.0-0-g5da58b9", dirty: true));
        }

        [Test]
        public void AfterReleaseTag_IsNextPatchDevBuild()
        {
            Assert.AreEqual("0.1.1-dev.12+5da58b9", GameVersion.FromDescribe("v0.1.0-12-g5da58b9\n", dirty: false));
            Assert.AreEqual("1.4.3-dev.1+abcdef0.dirty", GameVersion.FromDescribe("v1.4.2-1-gabcdef0", dirty: true));
        }

        [Test]
        public void UpstreamOrMalformedTags_AreNotVersions()
        {
            Assert.IsNull(GameVersion.FromDescribe("0.0.7.6-3-g5da58b9", dirty: false));
            Assert.IsNull(GameVersion.FromDescribe("develop-nightly-3-g5da58b9", dirty: false));
            Assert.IsNull(GameVersion.FromDescribe(null, dirty: false));
        }

        [Test]
        public void BeforeFirstReleaseTag_CountsAllCommits()
        {
            Assert.AreEqual("0.1.0-dev.1764+5da58b9", GameVersion.Untagged(1764, "5da58b9", dirty: false));
        }

        [Test]
        public void BuildInfoParse_ReadsVersion()
        {
            BuildInfo.Data data = BuildInfo.Parse("{\"version\":\"0.1.1-dev.2+007c4e9\",\"commit\":\"007c4e9\"}");
            Assert.AreEqual("0.1.1-dev.2+007c4e9", data.version);
        }
    }
}
