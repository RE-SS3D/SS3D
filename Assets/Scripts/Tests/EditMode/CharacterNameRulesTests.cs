using NUnit.Framework;
using SS3D.Systems.Entities.Character;

namespace EditorTests
{
    /// <summary>Lobby effort Decision 4: the name rules the creator and the server share.</summary>
    public class CharacterNameRulesTests
    {
        [TestCase("ada lovelace", "Ada Lovelace")]
        [TestCase("  jean-luc   o'neill ", "Jean-Luc O'neill")]
        [TestCase("MARY SUE", "Mary Sue")]
        [TestCase("Ronan McAllister", "Ronan McAllister")]
        [TestCase("anna-MARIA van-dijk", "Anna-Maria Van-Dijk")]
        public void Normalize_FixesCapitalisationAndSpacing(string input, string expected)
        {
            Assert.AreEqual(expected, CharacterNameRules.Normalize(input));
        }

        [TestCase("Ada Lovelace", CharacterNameError.None)]
        [TestCase("Jo Li", CharacterNameError.None)]
        [TestCase("Zoë D'Souza", CharacterNameError.None)]
        [TestCase("", CharacterNameError.Empty)]
        [TestCase("   ", CharacterNameError.Empty)]
        [TestCase(null, CharacterNameError.Empty)]
        [TestCase("A B", CharacterNameError.None)]
        [TestCase("Ab", CharacterNameError.TooShort)]
        [TestCase("Cher", CharacterNameError.NeedsTwoWords)]
        [TestCase("Ada -", CharacterNameError.NeedsTwoWords)]
        [TestCase("R2 D2", CharacterNameError.InvalidCharacters)]
        [TestCase("Ada <b>Lovelace</b>", CharacterNameError.InvalidCharacters)]
        [TestCase("Ada_Love Lace", CharacterNameError.InvalidCharacters)]
        [TestCase("Bartholomew Wolfgang Featherstonehaugh", CharacterNameError.TooLong)]
        public void Validate_AppliesTheRules(string input, CharacterNameError expected)
        {
            Assert.AreEqual(expected, CharacterNameRules.Validate(input));
        }

        [Test]
        public void MaxLength_IsCountedAfterNormalizing()
        {
            string name = "Abcdefghijklmno   Abcdefghijklmnop"; // 32 once the spaces collapse
            Assert.AreEqual(CharacterNameError.None, CharacterNameRules.Validate(name));
            Assert.AreEqual(CharacterNameRules.MaxLength, CharacterNameRules.Normalize(name).Length);
        }

        [Test]
        public void TryNormalize_EmptyOnFailure()
        {
            Assert.IsFalse(CharacterNameRules.TryNormalize("Cher", out string normalized, out CharacterNameError error));
            Assert.AreEqual(string.Empty, normalized);
            Assert.AreEqual(CharacterNameError.NeedsTwoWords, error);
            Assert.IsNotEmpty(CharacterNameRules.Describe(error));
        }
    }
}
