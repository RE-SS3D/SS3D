using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Roles;
using SS3D.UI.Lobby;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditorTests
{
    public class CharacterProfileStoreTests
    {
        private string _directory;
        private string ProfilePath => Path.Combine(_directory, "characters.json");

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "ss3d-profile-" + Guid.NewGuid());

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void NewStoreRestoresAllSlotsAndSelectedCharacter()
        {
            SavedCharacter first = new() { Id = "first", Name = "First", HairStyleIndex = 3, HairColorIndex = 4, SkinColorIndex = 5 };
            first.SetMorph("fat", 0.73f);
            SavedCharacter selected = new() { Id = "second", Name = "Second", BeardStyleIndex = 2, EyeColorIndex = 3, Eyebrows = "thick" };
            selected.SetMorph("height", 0.81f);
            Assert.That(new CharacterProfileStore(ProfilePath).Save(new SavedCharacter[] { first, selected }, selected.Id), Is.True);

            CharacterProfileStore.Profile loaded = new CharacterProfileStore(ProfilePath).Load();
            Assert.That(loaded.SelectedId, Is.EqualTo("second"));
            Assert.That(loaded.Characters.Length, Is.EqualTo(2));
            Assert.That(loaded.Characters[0].Name, Is.EqualTo("First"));
            Assert.That(loaded.Characters[0].HairStyleIndex, Is.EqualTo(3));
            Assert.That(loaded.Characters[0].HairColorIndex, Is.EqualTo(4));
            Assert.That(loaded.Characters[0].SkinColorIndex, Is.EqualTo(5));
            Assert.That(loaded.Characters[0].GetMorph("fat"), Is.EqualTo(0.73f));
            Assert.That(loaded.Characters[1].GetMorph("height"), Is.EqualTo(0.81f));
            Assert.That(loaded.Characters[1].BeardStyleIndex, Is.EqualTo(2));
            Assert.That(loaded.Characters[1].EyeColorIndex, Is.EqualTo(3));
            Assert.That(loaded.Characters[1].Eyebrows, Is.EqualTo("thick"));
        }

        [Test]
        public void UnsavedEditsDoNotChangeDiskAndSecondSaveReplacesFile()
        {
            SavedCharacter draft = new() { Id = "slot", Name = "Saved" };
            CharacterProfileStore store = new(ProfilePath);
            Assert.That(store.Save(new SavedCharacter[] { draft }, draft.Id), Is.True);
            draft.Name = "Edited";
            Assert.That(new CharacterProfileStore(ProfilePath).Load().Characters[0].Name, Is.EqualTo("Saved"));
            Assert.That(store.Save(new SavedCharacter[] { draft }, draft.Id), Is.True);
            Assert.That(new CharacterProfileStore(ProfilePath).Load().Characters[0].Name, Is.EqualTo("Edited"));
            Assert.That(File.Exists(ProfilePath + ".tmp"), Is.False);
        }

        [TestCase("invalid json")]
        [TestCase("{\"Version\":3,\"SelectedId\":\"a\",\"Characters\":[]}")]
        [TestCase("{\"Version\":2,\"Characters\":[]}")]
        [TestCase("{\"Version\":1,\"SelectedId\":\"missing\",\"Characters\":[]}")]
        public void UnreadableProfileIsPreservedWhenSavingDefaults(string original)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(ProfilePath, original);
            CharacterProfileStore store = new(ProfilePath);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Could not load character presets"));
            Assert.That(store.Load(), Is.Null);
            Assert.That(store.Save(new SavedCharacter[] { new SavedCharacter() }, "default"), Is.False);
            Assert.That(File.ReadAllText(ProfilePath), Is.EqualTo(original));
        }

        [Test]
        public void MissingProfileUsesDefaultsWithoutCreatingAFile()
        {
            Assert.That(new CharacterProfileStore(ProfilePath).Load(), Is.Null);
            Assert.That(File.Exists(ProfilePath), Is.False);
        }

        // --- Schema v2 (lobby effort Decisions 1–2) ---

        private static SavedCharacter RandomStub() => new() { Id = "random", Name = "Random Pick" };

        private void WriteV1(params string[] characterJson)
        {
            Directory.CreateDirectory(_directory);
            string entries = string.Join(",", characterJson);
            string selected = System.Text.RegularExpressions.Regex.Match(characterJson[^1], "\"Id\":\"([^\"]+)\"").Groups[1].Value;
            File.WriteAllText(ProfilePath, "{\"Version\":1,\"SelectedId\":\"" + selected + "\",\"Characters\":[" + entries + "]}");
        }

        // v1 files also carry Species and the random preview OutfitIndex, both since dropped; loading ignores them.
        private static string V1Entry(SavedCharacter draft) =>
            "{\"Draft\":{\"Species\":\"Human\",\"OutfitIndex\":3," + JsonUtility.ToJson(draft).Substring(1) + ",\"MorphKeys\":[\"height\"],\"MorphValues\":[" +
            draft.GetMorph("height").ToString(System.Globalization.CultureInfo.InvariantCulture) + "]}";

        [Test]
        public void PreferencesSurviveSaveAndLoad()
        {
            SavedCharacter draft = new() { Id = "a", Name = "Ada Lovelace" };
            draft.Preferences.SetPriority("Engineer", JobPriority.High);
            draft.Preferences.SetPriority("Doctor", JobPriority.Low);
            draft.Preferences.SetAntagonist(AntagonistCategories.NuclearOperative, true);
            draft.Preferences.Fallback = JobFallback.ReturnToLobby;
            Assert.That(new CharacterProfileStore(ProfilePath).Save(new[] { draft }, "a"), Is.True);

            CharacterPreferences loaded = new CharacterProfileStore(ProfilePath).Load().Characters[0].Preferences;
            Assert.That(loaded.PriorityOf("Engineer"), Is.EqualTo(JobPriority.High));
            Assert.That(loaded.PriorityOf("Doctor"), Is.EqualTo(JobPriority.Low));
            Assert.That(loaded.PriorityOf("Captain"), Is.EqualTo(JobPriority.Never));
            Assert.That(loaded.AntagonistChoice(AntagonistCategories.NuclearOperative), Is.True);
            Assert.That(loaded.Fallback, Is.EqualTo(JobFallback.ReturnToLobby));
            StringAssert.Contains("\"Version\": 2", File.ReadAllText(ProfilePath));
        }

        [Test]
        public void V1UntouchedStarterIsReplacedByARandomCharacter()
        {
            SavedCharacter starter = new();
            Assert.That(starter.IsUntouchedMockStarter, Is.True);
            WriteV1(V1Entry(starter));

            CharacterProfileStore.Profile profile = new CharacterProfileStore(ProfilePath, RandomStub).Load();

            Assert.That(profile.Migrated, Is.True);
            Assert.That(profile.Characters.Length, Is.EqualTo(1));
            Assert.That(profile.Characters[0].Name, Is.EqualTo("Random Pick"));
            Assert.That(profile.SelectedId, Is.EqualTo("random"));
            Assert.That(new CharacterProfileStore(ProfilePath).Load().Characters[0].Id, Is.EqualTo("random"), "the migration is written back");
        }

        [Test]
        public void V1EditedStarterKeepsItsLookWithNoPreferences()
        {
            SavedCharacter starter = new() { HairStyleIndex = 4 };
            starter.SetMorph("height", 0.8f);
            Assert.That(starter.IsUntouchedMockStarter, Is.False);
            WriteV1(V1Entry(starter));

            CharacterProfileStore.Profile profile = new CharacterProfileStore(ProfilePath, RandomStub).Load();

            SavedCharacter kept = profile.Characters.Single();
            Assert.That(kept.Id, Is.EqualTo(starter.Id));
            Assert.That(kept.HairStyleIndex, Is.EqualTo(4));
            Assert.That(kept.GetMorph("height"), Is.EqualTo(0.8f).Within(0.0001f));
            AssertNoPreferences(kept.Preferences);
        }

        [Test]
        public void V1WithSeveralCharactersKeepsThemAllAndTheSelection()
        {
            SavedCharacter starter = new();
            SavedCharacter second = new() { Id = "second", Name = "Bram Dekker", SkinColorIndex = 2 };
            WriteV1(V1Entry(starter), V1Entry(second));

            CharacterProfileStore.Profile profile = new CharacterProfileStore(ProfilePath, RandomStub).Load();

            Assert.That(profile.Characters.Select(c => c.Id), Is.EqualTo(new[] { starter.Id, "second" }),
                "the untouched starter stays when it isn't alone");
            Assert.That(profile.SelectedId, Is.EqualTo("second"));
            Assert.That(profile.Characters[1].SkinColorIndex, Is.EqualTo(2));
            foreach (SavedCharacter character in profile.Characters) AssertNoPreferences(character.Preferences);
        }

        [Test]
        public void MalformedV2PreferencesArePreserved()
        {
            Directory.CreateDirectory(_directory);
            string original = "{\"Version\":2,\"SelectedId\":\"a\",\"Characters\":[{\"Draft\":{\"Id\":\"a\",\"Name\":\"Ada Lovelace\"," +
                "\"Preferences\":{\"Jobs\":[\"Engineer\"],\"Priorities\":[],\"AntagonistOptIns\":[],\"AntagonistOptOuts\":[],\"Fallback\":0}}," +
                "\"MorphKeys\":[],\"MorphValues\":[]}]}";
            File.WriteAllText(ProfilePath, original);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Could not load character presets"));
            Assert.That(new CharacterProfileStore(ProfilePath).Load(), Is.Null);
            Assert.That(File.ReadAllText(ProfilePath), Is.EqualTo(original));
        }

        private static void AssertNoPreferences(CharacterPreferences preferences)
        {
            Assert.That(preferences.ToDictionary(), Is.Empty, "every job starts at Never");
            Assert.That(preferences.AntagonistChoice(AntagonistCategories.NuclearOperative), Is.Null, "antagonists start unset (off)");
            Assert.That(preferences.IsAntagonistOn(AntagonistCategories.NuclearOperative, false), Is.False);
            Assert.That(preferences.Fallback, Is.EqualTo(JobFallback.Assistant));
        }
    }
}
