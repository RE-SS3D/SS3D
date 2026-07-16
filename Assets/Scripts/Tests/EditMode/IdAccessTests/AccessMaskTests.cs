using NUnit.Framework;
using SS3D.Systems.IdAccess;

namespace EditorTests
{
    public class AccessMaskTests
    {
        [Test]
        public void HasAll_ReturnsTrueWhenCredentialContainsEveryRequiredBit()
        {
            AccessMask credential = AccessMask.FromLevels(
                AccessLevel.Security,
                AccessLevel.Crew,
                AccessLevel.Maintenance);

            AccessMask required = AccessMask.FromLevels(AccessLevel.Security, AccessLevel.Crew);

            Assert.IsTrue(credential.HasAll(required));
        }

        [Test]
        public void HasAll_ReturnsFalseWhenRequiredBitMissing()
        {
            AccessMask credential = AccessMask.FromLevels(AccessLevel.Civilian, AccessLevel.Crew);
            AccessMask required = AccessMask.FromLevels(AccessLevel.Security);

            Assert.IsFalse(credential.HasAll(required));
        }

        [Test]
        public void HasAny_ReturnsTrueWhenAnyBitMatches()
        {
            AccessMask credential = AccessMask.FromLevels(AccessLevel.Engineering);
            AccessMask required = AccessMask.FromLevels(AccessLevel.Security, AccessLevel.Engineering);

            Assert.IsTrue(credential.HasAny(required));
        }

        [Test]
        public void NoneRequirement_AlwaysPassesHasAll()
        {
            AccessMask credential = AccessMask.None;
            Assert.IsTrue(credential.HasAll(AccessMask.None));
        }
    }
}
