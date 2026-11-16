using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Roles;
using SS3D.UI.Lobby;
using UnityEditor;

namespace EditorTests
{
    /// <summary>Lobby effort Phase 1: the character roster, random characters and per-character preferences.</summary>
    public class CharacterRosterTests
    {
        private int _made;

        private SavedCharacter MakeRandom() => new() { Id = "r" + ++_made, Name = "Random Person" };

        [SetUp]
        public void SetUp() => _made = 0;

        [Test]
        public void EmptyRoster_StartsWithOneRandomCharacter()
        {
            CharacterRoster roster = new(null, null, MakeRandom);
            Assert.AreEqual(1, roster.Count);
            Assert.AreEqual("r1", roster.SelectedId);
        }

        [Test]
        public void AddAndDuplicate_StopAtTen()
        {
            CharacterRoster roster = new(null, null, MakeRandom);
            while (roster.CanAdd) Assert.IsNotNull(roster.AddRandom());

            Assert.AreEqual(CharacterRoster.MaxCharacters, roster.Count);
            Assert.IsNull(roster.AddRandom());
            Assert.IsNull(roster.Duplicate(roster.SelectedId));
            Assert.AreEqual(CharacterRoster.MaxCharacters, roster.Count);
        }

        [Test]
        public void Duplicate_CopiesLookAndPreferences_UnderANewId_AndSelectsIt()
        {
            SavedCharacter source = new() { Id = "a", Name = "Ada Lovelace", HairStyleIndex = 3 };
            source.SetMorph("fat", 0.7f);
            source.Preferences.SetPriority("Doctor", JobPriority.High);
            CharacterRoster roster = new(new[] { source, new SavedCharacter { Id = "b", Name = "Bram Dekker" } }, "a", MakeRandom);

            SavedCharacter copy = roster.Duplicate("a");

            Assert.AreNotEqual("a", copy.Id);
            Assert.AreEqual(copy.Id, roster.SelectedId);
            Assert.AreEqual(1, roster.Characters.ToList().IndexOf(copy), "the copy sits right after its source");
            Assert.AreEqual("Ada Lovelace", roster.LabelOf(copy.Id), "a character's name is its label; duplicates may share it");
            Assert.AreEqual(3, copy.HairStyleIndex);
            Assert.AreEqual(0.7f, copy.GetMorph("fat"));
            Assert.AreEqual(JobPriority.High, copy.Preferences.PriorityOf("Doctor"));

            copy.SetMorph("fat", 0.1f);
            copy.Preferences.SetPriority("Doctor", JobPriority.Low);
            Assert.AreEqual(0.7f, source.GetMorph("fat"), "the copy is deep");
            Assert.AreEqual(JobPriority.High, source.Preferences.PriorityOf("Doctor"));
        }

        [Test]
        public void Delete_KeepsTheLastCharacter_AndSelectsANeighbour()
        {
            CharacterRoster roster = new(new[]
            {
                new SavedCharacter { Id = "a" }, new SavedCharacter { Id = "b" }, new SavedCharacter { Id = "c" },
            }, "b", MakeRandom);

            Assert.IsTrue(roster.Delete("b"));
            Assert.AreEqual("c", roster.SelectedId, "the next one");
            Assert.IsTrue(roster.Delete("c"));
            Assert.AreEqual("a", roster.SelectedId, "the previous one when it was last");
            Assert.IsFalse(roster.CanDelete);
            Assert.IsFalse(roster.Delete("a"));
            Assert.AreEqual(1, roster.Count);
        }

        [Test]
        public void SetPriority_AllowsOneHigh()
        {
            CharacterPreferences preferences = new();
            preferences.SetPriority("Engineer", JobPriority.High);
            preferences.SetPriority("Doctor", JobPriority.High);

            Assert.AreEqual(JobPriority.Medium, preferences.PriorityOf("Engineer"), "the old High drops to Medium");
            Assert.AreEqual(JobPriority.High, preferences.PriorityOf("Doctor"));

            preferences.SetPriority("Doctor", JobPriority.Never);
            CollectionAssert.AreEquivalent(new[] { "Engineer" }, preferences.ToDictionary().Keys);
        }

        [Test]
        public void ToMessage_CarriesPrioritiesChoicesAndFallback()
        {
            CharacterPreferences preferences = new() { Fallback = JobFallback.ReturnToLobby };
            preferences.SetPriority("Security", JobPriority.Medium);
            preferences.SetAntagonist(AntagonistCategories.NuclearOperative, false);

            var message = preferences.ToMessage();

            CollectionAssert.AreEqual(new[] { "Security" }, message.Jobs);
            CollectionAssert.AreEqual(new[] { (byte)JobPriority.Medium }, message.Priorities);
            CollectionAssert.IsEmpty(message.AntagonistOptIns);
            CollectionAssert.AreEqual(new[] { AntagonistCategories.NuclearOperative }, message.AntagonistOptOuts);
            Assert.AreEqual((byte)JobFallback.ReturnToLobby, message.Fallback);
        }

        [Test]
        public void RandomCharacter_HasAValidNameAndNoPreferences()
        {
            CharacterNameList names = CharacterNameList.Create(new[] { "ada" }, new[] { "o'hara" });
            RandomCharacter.LookOptions look = new(hairStyles: 5, beardStyles: 3, hairColors: 4,
                eyebrows: new[] { "HumanEyebrowsBold" }, skinColors: 8, eyeColors: 8);

            for (int seed = 0; seed < 20; seed++)
            {
                SavedCharacter draft = RandomCharacter.Create(names, look, new System.Random(seed));
                Assert.AreEqual("Ada O'hara", draft.Name);
                Assert.That(draft.HairStyleIndex, Is.InRange(1, 4));
                Assert.That(draft.BeardStyleIndex, Is.InRange(0, 2));
                Assert.That(draft.HairColorIndex, Is.InRange(0, 3));
                Assert.AreEqual("humaneyebrowsbold", draft.Eyebrows);
                Assert.That(draft.GetMorph("height"), Is.InRange(0f, 1f));
                Assert.IsFalse(draft.IsUntouchedMockStarter);
                Assert.IsEmpty(draft.Preferences.ToDictionary());
                Assert.IsNull(draft.Preferences.AntagonistChoice(AntagonistCategories.NuclearOperative));
                if (draft.GetMorph("female") > 0.5f) Assert.AreEqual(0, draft.BeardStyleIndex);
            }
        }

        [Test]
        public void ShippedNameList_HasTwoHundredEachAndEveryPairIsValid()
        {
            CharacterNameList names = AssetDatabase.LoadAssetAtPath<CharacterNameList>(CharacterNameList.AssetPath);
            Assert.IsNotNull(names, CharacterNameList.AssetPath);
            Assert.That(names.FirstNames.Length, Is.GreaterThanOrEqualTo(200));
            Assert.That(names.LastNames.Length, Is.GreaterThanOrEqualTo(200));
            Assert.AreEqual(names.FirstNames.Length, names.FirstNames.Distinct().Count(), "first names are unique");
            Assert.AreEqual(names.LastNames.Length, names.LastNames.Distinct().Count(), "last names are unique");

            string longestFirst = names.FirstNames.OrderByDescending(n => n.Length).First();
            string longestLast = names.LastNames.OrderByDescending(n => n.Length).First();
            Assert.IsTrue(CharacterNameRules.IsValid(longestFirst + " " + longestLast), "the longest pair fits");
            foreach (string first in names.FirstNames)
                Assert.IsTrue(CharacterNameRules.IsValid(first + " " + longestLast), first);
            foreach (string last in names.LastNames)
                Assert.IsTrue(CharacterNameRules.IsValid(longestFirst + " " + last), last);
            foreach (string name in names.FirstNames.Concat(names.LastNames))
                Assert.AreEqual(name, CharacterNameRules.Normalize(name), "names are stored already capitalised: " + name);
        }

        [Test]
        public void LobbyCatalog_ReferencesTheNameList()
        {
            LobbyAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<LobbyAssetCatalog>(LobbyAssetPaths.CatalogAssetPath);
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.CharacterNames, "rebuild the catalog: SS3D → Data → Rebuild All UI Catalogs");
        }
    }
}
