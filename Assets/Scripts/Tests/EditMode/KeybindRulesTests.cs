using NUnit.Framework;
using SS3D.Systems.Inputs;

namespace EditorTests
{
    public class KeybindRulesTests
    {
        private const int Gameplay = 1;
        private const int BuildMenu = 2;

        private static KeybindRow Row(string id, string path, int contexts, string modifier = null, bool readOnly = false) =>
            new(id, id, "Map", path, modifier, contexts, readOnly);

        [Test]
        public void RebindingToAFreeKeyChangesOnlyThatRow()
        {
            KeybindRow[] rows = { Row("Use", "<Keyboard>/e", Gameplay), Row("Drop", "<Keyboard>/q", Gameplay) };

            KeybindResult result = KeybindRules.Rebind(rows, "Use", "<Keyboard>/g");

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Changes, Is.EqualTo(new[] { new KeybindChange("Use", "<Keyboard>/g") }));
            Assert.That(result.Notice, Is.Null);
        }

        [Test]
        public void ConflictWithACoActiveRowSwapsTheTwoKeys()
        {
            KeybindRow[] rows = { Row("Use", "<Keyboard>/e", Gameplay), Row("Drop", "<Keyboard>/q", Gameplay) };

            KeybindResult result = KeybindRules.Rebind(rows, "Drop", "<Keyboard>/e");

            Assert.That(result.Changes, Is.EquivalentTo(new[]
            {
                new KeybindChange("Drop", "<Keyboard>/e"),
                new KeybindChange("Use", "<Keyboard>/q"),
            }));
            Assert.That(result.Notice, Is.EqualTo("Use is now Q"));
        }

        [Test]
        public void RowsNeverActiveTogetherMayShareAKey()
        {
            KeybindRow[] rows = { Row("Use", "<Keyboard>/e", Gameplay), Row("Rotate", "<Keyboard>/r", BuildMenu) };

            KeybindResult result = KeybindRules.Rebind(rows, "Rotate", "<Keyboard>/e");

            Assert.That(result.Changes, Is.EqualTo(new[] { new KeybindChange("Rotate", "<Keyboard>/e") }));
        }

        [Test]
        public void ModifierChordDoesNotConflictWithTheBareKey()
        {
            KeybindRow[] rows =
            {
                Row("Drop", "<Keyboard>/q", Gameplay),
                Row("Rotate Left", "<Keyboard>/e", Gameplay, modifier: "<Keyboard>/ctrl"),
            };

            KeybindResult result = KeybindRules.Rebind(rows, "Rotate Left", "<Keyboard>/q");

            Assert.That(result.Changes, Is.EqualTo(new[] { new KeybindChange("Rotate Left", "<Keyboard>/q") }));
        }

        [Test]
        public void KeyHeldByAReadOnlyRowIsRefused()
        {
            KeybindRow[] rows = { Row("Use", "<Keyboard>/e", Gameplay), Row("Examine", "<Keyboard>/leftShift", Gameplay, readOnly: true) };

            KeybindResult result = KeybindRules.Rebind(rows, "Use", "<Keyboard>/leftShift");

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Changes, Is.Empty);
            Assert.That(result.Notice, Is.EqualTo("Left Shift is reserved for Examine"));
        }

        [Test]
        public void ReadOnlyRowCannotBeRebound()
        {
            KeybindRow[] rows = { Row("Toggle Menu", "<Keyboard>/escape", Gameplay, readOnly: true) };

            Assert.That(KeybindRules.Rebind(rows, "Toggle Menu", "<Keyboard>/g").Accepted, Is.False);
        }

        [Test]
        public void SwapAlsoMovesEveryOtherRowThatHeldTheKey()
        {
            KeybindRow[] rows =
            {
                Row("Rotate Left", "<Keyboard>/q", Gameplay, modifier: "<Keyboard>/ctrl"),
                Row("Rotate Right", "<Keyboard>/e", Gameplay, modifier: "<Keyboard>/ctrl"),
                Row("Snap Right", "<Keyboard>/e", Gameplay, modifier: "<Keyboard>/ctrl"),
            };

            KeybindResult result = KeybindRules.Rebind(rows, "Rotate Left", "<Keyboard>/e");

            Assert.That(result.Changes, Is.EquivalentTo(new[]
            {
                new KeybindChange("Rotate Left", "<Keyboard>/e"),
                new KeybindChange("Rotate Right", "<Keyboard>/q"),
                new KeybindChange("Snap Right", "<Keyboard>/q"),
            }));
            Assert.That(result.Notice, Is.EqualTo("Rotate Right and Snap Right are now Ctrl + Q"));
        }

        [TestCase("<Keyboard>/q", "<Keyboard>/ctrl", "Ctrl + Q")]
        [TestCase("<Mouse>/forwardButton", null, "Forward")]
        [TestCase("<Keyboard>/capsLock", null, "Caps Lock")]
        public void KeysReadAsPlayersExpect(string path, string modifier, string text)
        {
            Assert.That(KeybindRules.DisplayName(path, modifier), Is.EqualTo(text));
        }
    }
}
