using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The Join picker (lobby.md §7, lobby effort Phase 6), opened by Join during a round. The open jobs with open/total
    /// counts and descriptions, "Joining as … Change", and the crew manifest by department; it follows the replicated job
    /// list live, so a chosen job that fills moves the choice and says so. Lives in <c>Lobby.uxml</c> (<c>lobby-join</c>)
    /// and is styled by <c>LobbyJoin.uss</c>. Keys: ↑/↓ change the job, Enter joins, Esc closes.
    /// </summary>
    public sealed class LobbyJoinPicker
    {
        /// <summary>After Join, the button waits this long for the spawn before it can be pressed again (a refused pick).</summary>
        private const long JoinRetryMilliseconds = 3000;

        private readonly LobbyJoinModel _model = new();
        private readonly LobbyAssetCatalog _catalog;
        private readonly VisualElement _root;
        private readonly Label _roundTime;
        private readonly Label _openCount;
        private readonly VisualElement _notice;
        private readonly Label _noticeText;
        private readonly ScrollView _jobList;
        private readonly VisualElement _portrait;
        private readonly Label _character;
        private readonly Button _confirm;
        private readonly Label _confirmTitle;
        private readonly Label _confirmSubtitle;
        private readonly Label _aboard;
        private readonly VisualElement _manifest;
        private readonly Dictionary<string, VisualElement> _jobs = new();
        private Func<string, string> _describe = JobRoster.DescriptionOf;
        private IReadOnlyList<CrewManifestEntry> _crew = Array.Empty<CrewManifestEntry>();
        private string _jobsSignature;
        private string _manifestSignature;
        private bool _joining;
        private IVisualElementScheduledItem _joinRetry;

        /// <summary>Join pressed with this job; the server re-checks the slot (Decision 11).</summary>
        public event Action<string> JoinRequested;
        /// <summary>"Change": the host opens the character switcher beside this button.</summary>
        public event Action<VisualElement> ChangeRequested;

        public LobbyJoinPicker(VisualElement root, LobbyAssetCatalog catalog)
        {
            _root = root;
            _catalog = catalog;
            _roundTime = root.Q<Label>("lobby-join-round-time");
            _openCount = root.Q<Label>("lobby-join-open-count");
            _notice = root.Q("lobby-join-notice");
            _noticeText = root.Q<Label>("lobby-join-notice-text");
            _jobList = root.Q<ScrollView>("lobby-join-job-list");
            _portrait = root.Q("lobby-join-portrait");
            _character = root.Q<Label>("lobby-join-character");
            _confirm = root.Q<Button>("lobby-join-confirm");
            _confirmTitle = root.Q<Label>("lobby-join-confirm-title");
            _confirmSubtitle = root.Q<Label>("lobby-join-confirm-subtitle");
            _aboard = root.Q<Label>("lobby-join-aboard");
            _manifest = root.Q("lobby-join-manifest");
            ChangeButton = root.Q<Button>("lobby-join-change");

            root.Q<Button>("lobby-join-close").clicked += Close;
            ChangeButton.clicked += () => ChangeRequested?.Invoke(ChangeButton);
            _confirm.clicked += Join;
            // A press on the dimmed backdrop, outside the card, closes the picker.
            root.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == root) Close();
            });
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        public VisualElement Root => _root;

        public LobbyJoinModel Model => _model;

        public Button ChangeButton { get; }

        public bool IsOpen => _root.ClassListContains("lobby-join--open");

        public void Open()
        {
            if (IsOpen) return;
            _model.Reset();
            SetJoining(false);
            _root.AddToClassList("lobby-join--open");
            _root.BringToFront();
            Paint();
            _root.schedule.Execute(() => _confirm.Focus()).ExecuteLater(1);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _root.RemoveFromClassList("lobby-join--open");
            SetJoining(false);
        }

        /// <summary>
        /// The replicated jobs and manifest, the role descriptions and the selected character. Rows are rebuilt only when
        /// a count or a crew row changed, so hover and focus survive the host's 5 Hz refresh.
        /// </summary>
        public void SetData(IReadOnlyList<JobSlotInfo> jobs, string fallbackJob, Func<string, string> describe,
            IReadOnlyList<CrewManifestEntry> manifest, CharacterPreferences preferences, string characterName)
        {
            _describe = job =>
            {
                string text = describe?.Invoke(job);
                return string.IsNullOrEmpty(text) ? JobRoster.DescriptionOf(job) : text;
            };
            _crew = manifest ?? Array.Empty<CrewManifestEntry>();
            _character.text = string.IsNullOrWhiteSpace(characterName) ? "Unnamed" : characterName.Trim();
            string before = _model.Selected;
            _model.Set(jobs, fallbackJob, preferences);

            string jobsSignature = JobsSignature(jobs, fallbackJob);
            string manifestSignature = ManifestSignature(_crew);
            bool rebuild = jobsSignature != _jobsSignature || manifestSignature != _manifestSignature;
            _jobsSignature = jobsSignature;
            _manifestSignature = manifestSignature;
            if (rebuild) Paint();
            else if (before != _model.Selected) Refresh();
        }

        public void SetRoundTime(string clock) => _roundTime.text = clock;

        /// <summary>The live render of the selected character, the same texture the lobby card shows.</summary>
        public void SetPortrait(Texture texture)
        {
            _portrait.style.backgroundImage = texture switch
            {
                RenderTexture rt => Background.FromRenderTexture(rt),
                Texture2D tex => new StyleBackground(tex),
                _ => StyleKeyword.None,
            };
        }

        private static string JobsSignature(IReadOnlyList<JobSlotInfo> jobs, string fallback)
        {
            StringBuilder signature = new(fallback);
            if (jobs != null)
                foreach (JobSlotInfo job in jobs)
                    signature.Append(';').Append(job.Name).Append('/').Append(job.Department).Append('/').Append(job.Slots).Append('/').Append(job.Assigned);
            return signature.ToString();
        }

        private static string ManifestSignature(IReadOnlyList<CrewManifestEntry> crew)
        {
            StringBuilder signature = new();
            foreach (CrewManifestEntry entry in crew) signature.Append(entry.Name).Append('/').Append(entry.Job).Append(';');
            return signature.ToString();
        }

        // ---- Painting ----

        private void Paint()
        {
            BuildJobs();
            BuildManifest();
            Refresh();
        }

        private void BuildJobs()
        {
            _jobs.Clear();
            _jobList.Clear();
            foreach (JobSlotInfo job in _model.OpenJobs)
            {
                VisualElement item = BuildJob(job);
                _jobs[job.Name] = item;
                _jobList.Add(item);
            }

            if (_model.OpenJobs.Count == 0)
                _jobList.Add(LobbyUi.Text("The job list arrives from the server once you're connected.", "lobby-join__empty"));
        }

        private VisualElement BuildJob(JobSlotInfo job)
        {
            string name = job.Name;
            Department department = _model.DepartmentOf(name);
            string dept = LobbyView.DepartmentClass(department);
            VisualElement item = LobbyUi.Box("lobby-join__job-item");
            item.name = "lobby-join-job-" + name;

            Button row = new(() => Pick(name)) { name = "lobby-join-job" };
            row.AddToClassList("lobby-join__job");
            VisualElement ring = LobbyUi.Box("lobby-join__ring");
            ring.Add(LobbyUi.Box("lobby-join__ring-dot"));
            row.Add(ring);
            VisualElement frame = LobbyUi.Box("lobby-join__job-frame");
            if (dept != null) frame.AddToClassList("lobby-join__job-frame--" + department.ToString().ToLowerInvariant());
            frame.Add(CharacterJobsTab.JobIcon(_catalog, name, department));
            row.Add(frame);
            VisualElement text = LobbyUi.Box("lobby-join__job-text");
            text.Add(LobbyUi.Text(name, "lobby-join__job-name"));
            Label departmentLabel = LobbyUi.Text(department == Department.None ? "Other" : department.ToString(), "lobby-join__job-dept");
            if (dept != null) departmentLabel.AddToClassList(dept);
            text.Add(departmentLabel);
            row.Add(text);
            row.Add(LobbyUi.Box("lobby__grow"));
            string count = _model.CountText(job);
            if (count.Length > 0)
            {
                row.Add(LobbyUi.Mono(count, "lobby-join__job-count"));
                row.Add(LobbyUi.Text("open", "lobby-join__job-count-word"));
            }
            else
            {
                row.Add(LobbyUi.Text("unlimited", "lobby-join__job-count-word"));
            }

            item.Add(row);
            Label description = LobbyUi.Text(_describe(name), "lobby-join__description");
            description.name = "lobby-join-description";
            item.Add(description);
            return item;
        }

        private void BuildManifest()
        {
            _manifest.Clear();
            List<JoinManifestGroup> groups = _model.Manifest(_crew);
            _aboard.text = $"{_crew.Count} aboard";
            if (groups.Count == 0)
            {
                _manifest.Add(LobbyUi.Text("Nobody is aboard yet.", "lobby-join__empty"));
                return;
            }

            List<List<JoinManifestGroup>> columns = LobbyJoinModel.SplitColumns(groups);
            for (int c = 0; c < columns.Count; c++)
            {
                VisualElement column = LobbyUi.Box("lobby-join__column");
                column.EnableInClassList("lobby-join__column--left", c == 0);
                foreach (JoinManifestGroup group in columns[c]) column.Add(BuildGroup(group));
                _manifest.Add(column);
            }
        }

        private static VisualElement BuildGroup(JoinManifestGroup group)
        {
            Department department = group.Department;
            string key = department == Department.None ? "other" : department.ToString().ToLowerInvariant();
            string dept = LobbyView.DepartmentClass(department);
            VisualElement box = LobbyUi.Box("lobby-join__group");
            box.name = "lobby-join-dept-" + key;
            VisualElement head = LobbyUi.Box("lobby-join__group-head");
            VisualElement icon = LobbyUi.Icon("dept-" + (department == Department.None ? "civilian" : key));
            icon.AddToClassList("lobby-join__group-icon");
            icon.AddToClassList("lb-tint");
            if (dept != null) icon.AddToClassList(dept);
            head.Add(icon);
            Label title = LobbyUi.Titling(department == Department.None ? "Other" : department.ToString(), "lobby-join__group-name");
            if (dept != null) title.AddToClassList(dept);
            head.Add(title);
            head.Add(LobbyUi.Box("lobby__grow"));
            head.Add(LobbyUi.Mono(group.Crew.Count.ToString(), "lobby-join__group-count"));
            box.Add(head);
            foreach (CrewManifestEntry entry in group.Crew)
            {
                VisualElement row = LobbyUi.Box("lobby-join__crew");
                row.Add(LobbyUi.Text(entry.Name, "lobby-join__crew-name"));
                row.Add(LobbyUi.Text(entry.Job, "lobby-join__crew-job"));
                box.Add(row);
            }

            foreach (string job in group.Missing)
            {
                Label missing = LobbyUi.Text($"No {job} aboard", "lobby-join__crew-missing");
                missing.name = "lobby-join-missing-" + job;
                box.Add(missing);
            }

            return box;
        }

        /// <summary>Repaints the choice, the description, the notice and the Join button without rebuilding.</summary>
        private void Refresh()
        {
            // Only the chosen job shows its description; hover doesn't (owner feedback: rows jumped under the pointer).
            foreach ((string job, VisualElement item) in _jobs)
            {
                bool on = job == _model.Selected;
                item.EnableInClassList("lobby-join__job-item--on", on);
                Label description = item.Q<Label>("lobby-join-description");
                description.style.display = on && description.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _openCount.text = _model.OpenCountText;
            JoinNoticeKind notice = _model.Notice;
            _noticeText.text = _model.NoticeText;
            _notice.EnableInClassList("lobby-join__notice--hidden", notice == JoinNoticeKind.None);
            _notice.EnableInClassList("lobby-join__notice--warning", notice == JoinNoticeKind.ChosenFilled);
            _confirmTitle.text = _model.JoinTitle.ToUpperInvariant();
            _confirm.SetEnabled(_model.Selected != null && !_joining);
        }

        // ---- Actions ----

        private void Pick(string job)
        {
            if (_model.Select(job)) Refresh();
        }

        private void Join()
        {
            if (_joining || _model.Selected == null) return;
            SetJoining(true);
            JoinRequested?.Invoke(_model.Selected);
        }

        /// <summary>
        /// Pressed Join waits for the spawn, which hides the lobby. A refused pick (the job filled first) arrives as a
        /// private OOC line, so the button comes back after a few seconds.
        /// </summary>
        private void SetJoining(bool joining)
        {
            _joining = joining;
            _joinRetry?.Pause();
            _confirmSubtitle.text = joining ? "Joining…" : "Spawns you on the station straight away";
            if (joining)
            {
                _joinRetry = _confirm.schedule.Execute(() => SetJoining(false));
                _joinRetry.ExecuteLater(JoinRetryMilliseconds);
            }

            _confirm.SetEnabled(_model.Selected != null && !joining);
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape) Close();
            else if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                // Close and Change keep Enter for themselves.
                if (e.target is Button button && (button == ChangeButton || button.name == "lobby-join-close")) return;
                Join();
            }
            else if (e.keyCode is KeyCode.UpArrow or KeyCode.DownArrow) Step(e.keyCode == KeyCode.UpArrow ? -1 : 1);
            else return;

            e.StopPropagation();
        }

        private void Step(int direction)
        {
            IReadOnlyList<JobSlotInfo> open = _model.OpenJobs;
            if (open.Count == 0) return;
            int index = 0;
            for (int i = 0; i < open.Count; i++)
                if (open[i].Name == _model.Selected) index = i;
            Pick(open[Mathf.Clamp(index + direction, 0, open.Count - 1)].Name);
        }
    }
}
