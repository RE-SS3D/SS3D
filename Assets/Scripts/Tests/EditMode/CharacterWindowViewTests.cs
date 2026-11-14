using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;
using SS3D.UI.Lobby;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Lobby effort Phase 5: the character window binds <c>CharacterWindow.uxml</c> through the committed catalog and
    /// paints the selected character, its unsaved state, the Jobs and Antags tabs and the switcher. No panel: the tree is
    /// checked as built. Without a panel no change events fire, so edits go through the model and <c>Refresh</c>; the
    /// Play Mode pilots click the real controls.
    /// </summary>
    public sealed class CharacterWindowViewTests
    {
        private CharacterWindowView _view;
        private CharacterRoster _roster;
        private VisualElement _layer;
        private int _made;

        private static readonly List<JobSlotInfo> Playtest = new()
        {
            new JobSlotInfo { Name = "Captain", Department = (byte)Department.Command, Slots = 1 },
            new JobSlotInfo { Name = "Security", Department = (byte)Department.Security, Slots = 2 },
            new JobSlotInfo { Name = "Engineer", Department = (byte)Department.Engineering, Slots = 2 },
            new JobSlotInfo { Name = "Doctor", Department = (byte)Department.Medical, Slots = 1 },
            new JobSlotInfo { Name = "Assistant", Department = (byte)Department.Civilian, Slots = 0 },
        };

        private CharacterCreatorDraft Make(string name) => new() { Id = "c" + ++_made, Name = name };

        [SetUp]
        public void SetUp()
        {
            LobbyAssetCatalog catalog = Resources.Load<LobbyAssetCatalog>(LobbyAssetPaths.ResourcesCatalogName);
            Assert.IsNotNull(catalog, "The lobby catalog is committed under Resources");
            Assert.IsTrue(catalog.HasRequiredAssets(out string missing), "Catalog is missing " + missing);
            _made = 0;
            CharacterCreatorDraft rory = Make("Rory Lindqvist");
            rory.Preferences.SetPriority("Security", JobPriority.High);
            _roster = new CharacterRoster(new[] { rory, Make("Tomas Reyes") }, rory.Id, () => Make("Random Person"));
            _layer = new VisualElement();
            _view = new CharacterWindowView(catalog, _roster, _ => null,
                job => CharacterWindowModel.DepartmentOf(job, Playtest));
            _view.Attach(_layer);
            Assert.IsNotNull(_view.Root, "The window bound its template");
            _view.SetJobs(Playtest, job => job == "Captain" ? "Runs the station." : "", "Assistant");
            _view.Open();
        }

        [TearDown]
        public void TearDown() => _view.Detach();

        private string Status => _view.Root.Q<Label>("charwin-status-label").text;

        [Test]
        public void Open_ShowsTheSelectedCharacter_Saved()
        {
            Assert.AreSame(_layer, _view.Root.parent);
            Assert.Greater(_view.Root.styleSheets.count, 0, "CharacterWindow.uss is linked on the root");
            Assert.IsTrue(_view.IsVisible);
            Assert.AreEqual(CharacterWindowView.TabAppearance, _view.ActiveTab);
            Assert.AreEqual("Rory Lindqvist", _view.Root.Q<Label>("charwin-switch-name").text);
            Assert.AreEqual("Security", _view.Root.Q<Label>("charwin-switch-high-job").text);
            Assert.IsTrue(_view.Root.Q("charwin-switch-high-job").ClassListContains("lb-dept--security"));
            Assert.AreEqual("2/10", _view.Root.Q<Label>("charwin-switch-count").text);
            Assert.AreEqual("Rory Lindqvist", _view.Root.Q<TextField>("charwin-name").value);
            Assert.AreEqual("Saved", Status);
            Assert.IsFalse(_view.Root.Q<Button>("charwin-save").enabledSelf);
            Assert.IsFalse(_view.Root.Q<Button>("charwin-discard").enabledSelf);
            Assert.AreEqual("Wearing Security clothes, from your High job.", _view.Root.Q<Label>("charwin-outfit-caption").text);
        }

        [Test]
        public void AnEdit_IsUnsaved_UntilSaved()
        {
            CharacterCreatorDraft stored = null;
            _view.Saved += draft => stored = draft;
            _view.Model.Working.SetMorph("height", 0.9f);
            _view.Refresh();
            Assert.AreEqual("Unsaved changes", Status);
            Assert.IsTrue(_view.Root.Q<Button>("charwin-save").enabledSelf);

            Assert.IsTrue(_view.Save());
            Assert.IsNotNull(stored);
            Assert.AreEqual(0.9f, stored.GetMorph("height"), 0.0001f);
            Assert.AreEqual("Saved", Status);
        }

        [Test]
        public void InvalidName_ShowsTheError_AndDisablesSave()
        {
            _view.Model.Working.Name = "Rory";
            _view.Refresh();
            Assert.IsFalse(_view.Root.Q("charwin-name-error").ClassListContains("charwin-look__error--hidden"));
            Button save = _view.Root.Q<Button>("charwin-save");
            Assert.IsFalse(save.enabledSelf);
            Assert.AreEqual("Enter a first and a last name.", save.tooltip);
            Assert.IsFalse(_view.Save());
        }

        [Test]
        public void Return_WithUnsavedChanges_AsksFirst_DiscardThenReturns()
        {
            bool returned = false;
            _view.ReturnRequested += () => returned = true;
            _view.Model.Working.SetMorph("fat", 0.7f);
            _view.RequestReturn();
            Assert.IsFalse(returned);
            Assert.IsTrue(_view.PromptOpen, "Save / Discard / Keep editing");

            _view.Discard();
            Assert.AreEqual("Saved", Status);
            _view.RequestReturn();
            Assert.IsTrue(returned, "Nothing unsaved: return at once");
        }

        [Test]
        public void JobsTab_ListsDepartments_TotalsAndTheFallback()
        {
            _view.SelectTab(CharacterWindowView.TabJobs);
            VisualElement jobs = _view.Root.Q("charwin-jobs");
            Assert.IsNotNull(jobs.Q("charwin-dept-command"));
            Assert.IsNotNull(jobs.Q("charwin-dept-civilian"));
            VisualElement security = jobs.Q("charwin-job-Security");
            Assert.AreEqual("2 slots", security.Q<Label>(className: "charwin-jobs__slots").text);
            Assert.IsTrue(security.Q<Button>("charwin-prio-high").ClassListContains("charwin-jobs__prio-option--high-on"));
            Assert.AreEqual("unlimited", jobs.Q("charwin-job-Assistant").Q<Label>(className: "charwin-jobs__slots").text);
            Assert.AreEqual(DisplayStyle.None, jobs.Q("charwin-job-Doctor").Q<Button>("charwin-job-about").style.display.value,
                "No description, no expander");
            Assert.IsTrue(jobs.Q<Button>("charwin-fallback-assistant").ClassListContains("charwin-jobs__radio--on"));
            Assert.IsTrue(jobs.Q("charwin-jobs-notice").ClassListContains("charwin__notice--hidden"));

            VisualElement chief = jobs.Q("charwin-job-Chief Engineer");
            Assert.IsNotNull(chief, "Every roster job shows, including ones the server doesn't run");
            Assert.IsTrue(chief.ClassListContains("charwin-jobs__row--locked"));
            Assert.IsNull(chief.Q("charwin-job-priority"), "A locked job has no priority control");
            Assert.AreEqual(CharacterJobsTab.NotInGameReason, chief.Q<Label>(className: "charwin-jobs__locked-text").text);
            Assert.IsNull(jobs.Q("charwin-job-Security Officer"), "The game's Security is the roster's Security Officer");
            Assert.IsNotNull(jobs.Q("charwin-dept-science"));
        }

        [Test]
        public void AntagsTab_StartsOff_OnAServerWhoseDefaultIsOff()
        {
            _view.SelectTab(CharacterWindowView.TabAntags);
            VisualElement row = _view.Root.Q("charwin-antag-" + AntagonistCategories.NuclearOperative);
            Assert.IsNotNull(row);
            Assert.IsFalse(row.ClassListContains("charwin-antags__row--on"));
            _view.SetAntagonistDefault(true);
            row = _view.Root.Q("charwin-antag-" + AntagonistCategories.NuclearOperative);
            Assert.IsTrue(row.ClassListContains("charwin-antags__row--on"), "-nukeops: unset means on");
        }

        [Test]
        public void Switcher_ListsCharacters_ConfirmsDelete_AndFillsAtTen()
        {
            CharacterSwitcher switcher = _view.Switcher;
            switcher.Refresh();
            Assert.AreEqual(2, switcher.Root.Query("character-switcher-row").ToList().Count);
            string other = _roster.Characters[1].Id;
            switcher.BeginDelete(other);
            Assert.AreEqual(other, switcher.ConfirmingDeleteId);
            StringAssert.Contains("Tomas Reyes", switcher.Root.Q("character-switcher-confirm").Q<Label>().text);

            while (_roster.CanAdd) _roster.AddRandom();
            switcher.Refresh();
            Assert.IsFalse(switcher.Root.Q<Button>("character-switcher-new").enabledSelf);
            Assert.IsFalse(switcher.Root.Q<Label>(className: "switcher__full").ClassListContains("switcher__full--hidden"));
        }
    }
}
