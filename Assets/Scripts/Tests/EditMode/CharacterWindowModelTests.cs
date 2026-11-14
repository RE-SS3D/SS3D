using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;
using SS3D.UI.Lobby;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Lobby effort Phase 5: the character window's rules. One Save covers the three tabs, an invalid name blocks it,
    /// one job can be High and the notice says what moved, and the preview wears the High job's clothes.
    /// </summary>
    public sealed class CharacterWindowModelTests
    {
        private static CharacterCreatorDraft Saved()
        {
            CharacterCreatorDraft draft = new() { Id = "a", Name = "Rory Lindqvist" };
            draft.Preferences.SetPriority("Janitor", JobPriority.High);
            return draft;
        }

        private static JobSlotInfo Job(string name, Department department, int slots = 1) =>
            new() { Name = name, Department = (byte)department, Slots = slots };

        [Test]
        public void Load_IsClean_EditsMakeItDirty_DiscardRestores()
        {
            CharacterWindowModel model = new();
            CharacterCreatorDraft saved = Saved();
            model.Load(saved);
            Assert.IsFalse(model.IsDirty);
            Assert.IsFalse(model.CanSave, "Nothing to save");
            Assert.AreNotSame(saved, model.Working, "The tabs edit a copy");

            model.Working.SetMorph("height", 0.9f);
            Assert.IsTrue(model.IsDirty);
            model.Discard();
            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(saved.GetMorph("height"), model.Working.GetMorph("height"));
        }

        [Test]
        public void EveryTab_CountsAsAChange()
        {
            CharacterWindowModel model = new();
            model.Load(Saved());
            model.Working.HairColorIndex++;
            Assert.IsTrue(model.IsDirty, "Appearance");

            model.Discard();
            model.SetPriority("Chef", JobPriority.Low);
            Assert.IsTrue(model.IsDirty, "Jobs");

            model.Discard();
            model.SetFallback(JobFallback.ReturnToLobby);
            Assert.IsTrue(model.IsDirty, "Fallback");

            model.Discard();
            model.Working.Preferences.SetAntagonist(AntagonistCategories.NuclearOperative, true);
            Assert.IsTrue(model.IsDirty, "Antags");
        }

        [Test]
        public void InvalidName_BlocksSave_WithTheReasonAsTooltip()
        {
            CharacterWindowModel model = new();
            model.Load(Saved());
            model.Working.Name = "Rory";
            Assert.IsTrue(model.IsDirty);
            Assert.IsFalse(model.CanSave);
            Assert.AreEqual(CharacterNameRules.Describe(CharacterNameError.NeedsTwoWords), model.SaveTooltip);
            Assert.IsNull(model.Commit());
        }

        [Test]
        public void Commit_NormalisesTheName_AndBecomesTheSavedCopy()
        {
            CharacterWindowModel model = new();
            CharacterCreatorDraft original = Saved();
            model.Load(original);
            model.Working.Name = "  ada   o'hara ";
            CharacterCreatorDraft stored = model.Commit();
            Assert.IsNotNull(stored);
            Assert.AreEqual("Ada O'hara", stored.Name);
            Assert.AreEqual("a", stored.Id, "Same character");
            Assert.AreSame(stored, model.Saved);
            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual("Rory Lindqvist", original.Name, "The old saved instance is left alone");
        }

        [Test]
        public void NewHigh_DropsTheOldHighToMedium_AndSaysSo()
        {
            CharacterWindowModel model = new();
            model.Load(Saved());
            model.SetPriority("Security", JobPriority.High);

            Assert.AreEqual(JobPriority.High, model.PriorityOf("Security"));
            Assert.AreEqual(JobPriority.Medium, model.PriorityOf("Janitor"));
            Assert.AreEqual(JobsNoticeKind.HighMoved, model.Notice);
            Assert.AreEqual("Janitor", model.MovedJob);
            StringAssert.Contains("<b>Security</b> is now High. <b>Janitor</b> moved from High to Medium", model.NoticeText("Assistant"));

            model.SetPriority("Chef", JobPriority.Low);
            Assert.AreEqual(JobsNoticeKind.None, model.Notice, "The next change clears the notice");
        }

        [Test]
        public void AllNever_IsExplained_PerFallback()
        {
            CharacterWindowModel model = new();
            model.Load(Saved());
            model.SetPriority("Janitor", JobPriority.Never);
            Assert.AreEqual(JobsNoticeKind.AllNever, model.Notice);
            StringAssert.Contains("you'll join as Assistant", model.NoticeText("Assistant"));

            model.SetFallback(JobFallback.ReturnToLobby);
            StringAssert.Contains("stay in the lobby", model.NoticeText("Assistant"));
        }

        [Test]
        public void Preview_WearsTheHighJob_OrTheFallbackJob()
        {
            CharacterWindowModel model = new();
            model.Load(Saved());
            Assert.AreEqual("Janitor", model.OutfitJob("Assistant"));
            Assert.AreEqual("Wearing Janitor clothes, from your High job.", model.OutfitCaption("Assistant", true));

            model.SetPriority("Janitor", JobPriority.Low);
            Assert.AreEqual("Assistant", model.OutfitJob("Assistant"));
            Assert.AreEqual("Wearing Assistant clothes: nothing is set to High.", model.OutfitCaption("Assistant", true));
            StringAssert.StartsWith("Clothing hidden", model.OutfitCaption("Assistant", false));
        }

        [Test]
        public void Slots_ShowTotalsOnly()
        {
            Assert.AreEqual("1 slot", CharacterWindowModel.SlotsText(Job("Captain", Department.Command)));
            Assert.AreEqual("4 slots", CharacterWindowModel.SlotsText(Job("Engineer", Department.Engineering, 4)));
            Assert.AreEqual("unlimited", CharacterWindowModel.SlotsText(Job("Assistant", Department.Civilian, 0)));
        }

        [Test]
        public void Departments_SplitIntoTwoColumnsOfAboutHalfTheJobs()
        {
            List<JobSlotInfo> playtest = new()
            {
                Job("Captain", Department.Command), Job("Security", Department.Security), Job("Engineer", Department.Engineering),
                Job("Doctor", Department.Medical), Job("Assistant", Department.Civilian, 0),
            };
            List<List<Department>> columns = CharacterJobsTab.SplitColumns(playtest);
            CollectionAssert.AreEqual(new[] { Department.Command, Department.Security, Department.Engineering }, columns[0]);
            CollectionAssert.AreEqual(new[] { Department.Medical, Department.Civilian }, columns[1]);
        }

        [Test]
        public void SameAs_IgnoresArrayOrder()
        {
            CharacterPreferences a = new();
            a.SetPriority("Chef", JobPriority.Low);
            a.SetPriority("Janitor", JobPriority.High);
            CharacterPreferences b = new();
            b.SetPriority("Janitor", JobPriority.High);
            b.SetPriority("Chef", JobPriority.Low);
            Assert.IsTrue(a.SameAs(b));
            b.SetPriority("Chef", JobPriority.Medium);
            Assert.IsFalse(a.SameAs(b));
        }

        [Test]
        public void RandomizeLook_KeepsTheNameAndPreferences()
        {
            CharacterCreatorDraft draft = Saved();
            RandomCharacter.RandomizeLook(draft, new RandomCharacter.LookOptions(5, 3, 4, new[] { "HumanEyebrowsBold" }, 1), new System.Random(3));
            Assert.AreEqual("Rory Lindqvist", draft.Name);
            Assert.AreEqual(JobPriority.High, draft.Preferences.PriorityOf("Janitor"));
            Assert.That(draft.HairStyleIndex, Is.InRange(1, 4));
        }
    }
}
