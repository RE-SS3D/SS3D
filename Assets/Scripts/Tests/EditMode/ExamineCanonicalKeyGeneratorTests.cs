using NUnit.Framework;
using SS3D.Systems.Examine;

namespace EditorTests
{
    public class ExamineCanonicalKeyGeneratorTests
    {
        private const string WrenchAssetPath =
            "Assets/Content/Data/Examine/String/Items/Functional/Tools/Engineering/Wrench.asset";

        private const string SteelWallAssetPath =
            "Assets/Content/Data/Examine/String/Structures/Walls/SteelWall.asset";

        private const string AtmosPipeAssetPath =
            "Assets/Content/Data/Examine/String/Structures/Pipes/AtmosPipesL1.asset";

        [Test]
        public void GetNameKeyUsesAssetPathForLegacyProseKeys()
        {
            string key = ExamineCanonicalKeyGenerator.GetNameKey(WrenchAssetPath, "Wrench");

            Assert.AreEqual("items.tools.engineering.wrench.name", key);
        }

        [Test]
        public void GetNameKeyUsesLegacySnakeCaseIdentifierForSharedNames()
        {
            string key = ExamineCanonicalKeyGenerator.GetNameKey(AtmosPipeAssetPath, "atmos_pipe");

            Assert.AreEqual("structures.pipes.atmos_pipe.name", key);
        }

        [Test]
        public void GetDescriptionKeyUsesAssetSpecificPath()
        {
            string key = ExamineCanonicalKeyGenerator.GetDescriptionKey(AtmosPipeAssetPath);

            Assert.AreEqual("structures.pipes.atmos_pipes_l1.desc", key);
        }

        [Test]
        public void GetNameKeyUsesFolderAndLegacyIdentifierForSnakeCaseKeys()
        {
            string key = ExamineCanonicalKeyGenerator.GetNameKey(SteelWallAssetPath, "steel_wall");

            Assert.AreEqual("structures.walls.steel_wall.name", key);
        }

        [Test]
        public void ToSnakeCaseSplitsPascalCaseAndDigits()
        {
            Assert.AreEqual("atmos_pipes_l1", ExamineCanonicalKeyGenerator.ToSnakeCase("AtmosPipesL1"));
            Assert.AreEqual("jumpsuit_botany", ExamineCanonicalKeyGenerator.ToSnakeCase("JumpsuitBotany"));
        }
    }
}
