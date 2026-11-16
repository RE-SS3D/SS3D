using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands;

namespace EditorTests
{
    public class IdAccessCommandUtilitiesTests
    {
        [Test]
        public void TryParseAccessLevel_AcceptsDisplayNameAndEnumName()
        {
            Assert.IsTrue(IdAccessCommandUtilities.TryParseAccessLevel("Engineering", out AccessLevel level, out string error));
            Assert.AreEqual(AccessLevel.Engineering, level);
            Assert.IsEmpty(error);

            Assert.IsTrue(IdAccessCommandUtilities.TryParseAccessLevel("Change ID", out level, out error));
            Assert.AreEqual(AccessLevel.ChangeId, level);
        }

        [Test]
        public void TryParseAccessLevel_RejectsUnknownLevel()
        {
            bool parsed = IdAccessCommandUtilities.TryParseAccessLevel("BridgeAccess", out _, out string error);

            Assert.IsFalse(parsed);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryParsePreset_MapsEngineerPreset()
        {
            Assert.IsTrue(IdAccessCommandUtilities.TryParsePreset("engineer", out AccessMask mask, out string error));
            Assert.IsTrue(mask.HasAll(AccessPresets.Engineer));
            Assert.IsEmpty(error);
        }

        [Test]
        public void FormatAccessMask_ListsGrantedLevels()
        {
            AccessMask mask = AccessMask.FromLevels(AccessLevel.Engineering, AccessLevel.Crew);
            string formatted = IdAccessCommandUtilities.FormatAccessMask(mask);

            StringAssert.Contains("Engineering", formatted);
            StringAssert.Contains("Crew", formatted);
        }
    }
}
