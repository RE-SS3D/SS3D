using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window's Jobs tab (lobby.md §2–§3, lobby effort Phase 5), wide over the viewport while open: the
    /// whole station roster by department (<see cref="JobRoster"/>), total slots only (Decision 12), a High / Medium /
    /// Low / Never segmented control per job under the one-High rule, expandable descriptions, locked rows for jobs this
    /// server doesn't run, the notice line and the pinned fallback choice. It edits
    /// <see cref="CharacterWindowModel.Working"/>.
    /// </summary>
    public sealed class CharacterJobsTab
    {
        /// <summary>The lock reason of a roster job with no role data on this server.</summary>
        public const string NotInGameReason = "Not in the game yet";

        private static readonly JobPriority[] Priorities = { JobPriority.High, JobPriority.Medium, JobPriority.Low, JobPriority.Never };

        /// <summary>A job with no pictogram of its own uses its department's symbol (canvas note on icons).</summary>
        private static readonly Dictionary<string, string> IconAliases = new()
        {
            { "Security", "SecurityOfficer" },
        };

        private readonly CharacterWindowModel _model;
        private readonly LobbyAssetCatalog _catalog;
        private readonly ScrollView _scroll;
        private readonly VisualElement _fallbackBar;
        private readonly Dictionary<string, VisualElement> _rows = new();
        private readonly HashSet<string> _expanded = new();
        private IReadOnlyList<JobSlotInfo> _jobs = Array.Empty<JobSlotInfo>();
        private Func<string, string> _describe = _ => string.Empty;
        private Func<string, string> _lockedReason = _ => null;
        private string _fallbackJob = "Assistant";
        private readonly VisualElement _notice;
        private readonly Label _noticeText;

        public event Action Changed;

        public CharacterJobsTab(CharacterWindowModel model, LobbyAssetCatalog catalog)
        {
            _model = model;
            _catalog = catalog;
            Root = LobbyUi.Box("charwin-jobs", "charwin__pane");
            Root.name = "charwin-jobs";
            Label intro = LobbyUi.Text("Rank the jobs you want. One job can be High; Medium and Low take as many as you like. "
                + "Everyone's choices are settled together when the round starts, so there is no rush.", "charwin__note");
            intro.AddToClassList("charwin-jobs__intro");
            Root.Add(intro);

            _notice = LobbyUi.Box("charwin__notice");
            _notice.name = "charwin-jobs-notice";
            _notice.Add(LobbyUi.Icon("info"));
            _noticeText = LobbyUi.Text("", "charwin__notice-text");
            _noticeText.enableRichText = true;
            _notice.Add(_noticeText);
            Root.Add(_notice);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("charwin-jobs__scroll");
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(_scroll);
            _fallbackBar = LobbyUi.Box("charwin-jobs__fallback");
            _fallbackBar.name = "charwin-fallback";
            Root.Add(_fallbackBar);
        }

        public VisualElement Root { get; }

        /// <summary>
        /// The server's jobs, merged into the full roster (a roster job the server doesn't run is locked as not in the
        /// game yet); their descriptions; the playtime lock reason (null when open; no job has a requirement yet, see
        /// the effort's deviations); and the uncapped fallback job.
        /// </summary>
        public void SetJobs(IReadOnlyList<JobSlotInfo> jobs, Func<string, string> describe, Func<string, string> lockedReason, string fallbackJob)
        {
            HashSet<string> notInGame = new();
            _jobs = JobRoster.Merge(jobs ?? Array.Empty<JobSlotInfo>(), notInGame);
            Func<string, string> serverDescription = describe ?? (_ => string.Empty);
            _describe = job =>
            {
                string text = serverDescription(job);
                return string.IsNullOrEmpty(text) ? JobRoster.DescriptionOf(job) : text;
            };
            Func<string, string> playtime = lockedReason ?? (_ => null);
            _lockedReason = job => notInGame.Contains(job) ? NotInGameReason : playtime(job);
            _fallbackJob = string.IsNullOrEmpty(fallbackJob) ? "Assistant" : fallbackJob;
        }

        public void Rebuild()
        {
            _rows.Clear();
            _scroll.Clear();
            if (_model.Working == null) return;

            if (_jobs.Count == 0)
            {
                _scroll.Add(LobbyUi.Text("The job list arrives from the server once you're connected.", "charwin-jobs__empty"));
            }
            else
            {
                _scroll.Add(BuildColumns());
            }

            BuildFallback();
            Refresh();
        }

        /// <summary>
        /// Departments in order, split in two columns of about equal rows: the first column takes departments until it
        /// holds half the jobs (the canvas's Jobs boards).
        /// </summary>
        public static List<List<Department>> SplitColumns(IReadOnlyList<JobSlotInfo> jobs)
        {
            List<Department> order = new();
            foreach (JobSlotInfo job in jobs)
            {
                Department department = (Department)job.Department;
                if (!order.Contains(department)) order.Add(department);
            }

            order.Sort();
            List<List<Department>> columns = new() { new List<Department>(), new List<Department>() };
            int leftRows = 0;
            foreach (Department department in order)
            {
                bool left = leftRows * 2 < jobs.Count;
                columns[left ? 0 : 1].Add(department);
                if (left) leftRows += jobs.Count(job => (Department)job.Department == department);
            }

            return columns;
        }

        private VisualElement BuildColumns()
        {
            VisualElement columns = LobbyUi.Box("charwin-jobs__columns");
            List<List<Department>> split = SplitColumns(_jobs);
            for (int c = 0; c < split.Count; c++)
            {
                VisualElement column = LobbyUi.Box("charwin-jobs__column");
                column.EnableInClassList("charwin-jobs__column--left", c == 0);
                foreach (Department department in split[c])
                {
                    column.Add(BuildDepartment(department));
                }

                columns.Add(column);
            }

            return columns;
        }

        private VisualElement BuildDepartment(Department department)
        {
            string dept = LobbyView.DepartmentClass(department);
            VisualElement box = LobbyUi.Box("charwin-jobs__dept");
            box.name = "charwin-dept-" + department.ToString().ToLowerInvariant();
            // The department colour (--dept) reaches its edge, glyph, name and its jobs' High.
            if (dept != null) box.AddToClassList(dept);
            VisualElement head = LobbyUi.Box("charwin-jobs__dept-head");
            VisualElement icon = DepartmentIcon(department);
            icon.AddToClassList("charwin-jobs__dept-icon");
            head.Add(icon);
            head.Add(LobbyUi.Text(department == Department.None ? "other" : department.ToString().ToLowerInvariant(),
                "charwin-jobs__dept-name"));
            box.Add(head);
            foreach (JobSlotInfo job in _jobs.Where(job => (Department)job.Department == department))
            {
                box.Add(BuildRow(job, department));
            }

            return box;
        }

        private static VisualElement DepartmentIcon(Department department)
        {
            string dept = LobbyView.DepartmentClass(department);
            VisualElement icon = LobbyUi.Icon("dept-" + (department == Department.None ? "civilian" : department.ToString().ToLowerInvariant()));
            icon.AddToClassList("lb-tint");
            if (dept != null) icon.AddToClassList(dept);
            return icon;
        }

        private VisualElement BuildRow(JobSlotInfo job, Department department)
        {
            string name = job.Name;
            VisualElement row = LobbyUi.Box("charwin-jobs__row");
            row.name = "charwin-job-" + name;
            row.tooltip = name;
            VisualElement main = LobbyUi.Box("charwin-jobs__row-main");

            main.Add(JobIcon(_catalog, name, department));
            main.Add(LobbyUi.Text(name, "charwin-jobs__job-name"));

            string description = _describe(name);
            Button about = new() { name = "charwin-job-about", tooltip = "About " + name };
            about.AddToClassList("charwin-jobs__about");
            about.Add(LobbyUi.Icon("chevron-right"));
            about.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;
            main.Add(about);
            main.Add(LobbyUi.Box("lobby__grow"));
            string locked = _lockedReason(name);
            // A job not in the game has no slots to count.
            if (locked != NotInGameReason) main.Add(LobbyUi.Mono(CharacterWindowModel.SlotsText(job), "charwin-jobs__slots"));

            if (locked != null)
            {
                row.AddToClassList("charwin-jobs__row--locked");
                row.tooltip = name + ": " + locked;
                VisualElement lockBox = LobbyUi.Box("charwin-jobs__locked");
                lockBox.Add(LobbyUi.Text(locked, "charwin-jobs__locked-text"));
                main.Add(lockBox);
            }
            else
            {
                main.Add(BuildPriorityGroup(name));
            }

            row.Add(main);

            Label text = LobbyUi.Text(description, "charwin-jobs__description");
            text.name = "charwin-job-description";
            row.Add(text);
            about.clicked += () =>
            {
                if (!_expanded.Remove(name)) _expanded.Add(name);
                Refresh();
            };
            _rows[name] = row;
            return row;
        }

        /// <summary>A job's pictogram tinted in its department colour, or the department symbol when it has none.</summary>
        internal static VisualElement JobIcon(LobbyAssetCatalog catalog, string job, Department department)
        {
            string id = IconAliases.TryGetValue(job, out string alias) ? alias : job.Replace(" ", "");
            VisualElement icon;
            if (catalog != null && catalog.TryGetJobIcon(id, out Sprite sprite) && sprite != null)
            {
                icon = LobbyUi.Box("lobby__icon");
                icon.pickingMode = PickingMode.Ignore;
                icon.style.backgroundImage = new StyleBackground(sprite);
            }
            else
            {
                icon = DepartmentIcon(department);
            }

            icon.AddToClassList("charwin-jobs__job-icon");
            icon.AddToClassList("lb-tint");
            string dept = LobbyView.DepartmentClass(department);
            if (dept != null) icon.AddToClassList(dept);
            return icon;
        }

        /// <summary>The reference's segmented control as a radio group: Tab reaches it, ←/→ change it, a click picks one.</summary>
        private VisualElement BuildPriorityGroup(string job)
        {
            VisualElement group = LobbyUi.Box("charwin__seg");
            group.AddToClassList("charwin-jobs__prio");
            group.name = "charwin-job-priority";
            group.focusable = true;
            group.tooltip = "Priority for " + job + " (←/→)";
            foreach (JobPriority priority in Priorities)
            {
                Button option = new(() => Set(job, priority))
                {
                    name = "charwin-prio-" + Key(priority),
                    text = priority == JobPriority.Medium ? "med" : Key(priority),
                    focusable = false,
                };
                option.AddToClassList("charwin__seg-option");
                option.AddToClassList("charwin-jobs__prio-option");
                // Per level, so each can look its own (High previews the department's colour on hover).
                option.AddToClassList("charwin-jobs__prio-option--" + Key(priority));
                group.Add(option);
            }

            group.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.LeftArrow or KeyCode.RightArrow)) return;
                int index = Array.IndexOf(Priorities, _model.PriorityOf(job));
                int next = Mathf.Clamp(index + (e.keyCode == KeyCode.LeftArrow ? -1 : 1), 0, Priorities.Length - 1);
                Set(job, Priorities[next]);
                e.StopPropagation();
            });
            return group;
        }

        private void Set(string job, JobPriority priority)
        {
            _model.SetPriority(job, priority);
            Refresh();
            string moved = _model.MovedJob;
            if (moved != null && _rows.TryGetValue(moved, out VisualElement row))
            {
                row.AddToClassList("charwin-jobs__row--flash");
                row.schedule.Execute(() => row.RemoveFromClassList("charwin-jobs__row--flash")).ExecuteLater(1200);
            }

            Changed?.Invoke();
        }

        private void BuildFallback()
        {
            _fallbackBar.Clear();
            _fallbackBar.Add(LobbyUi.Text("if none of my jobs is open", "charwin-jobs__fallback-label"));
            VisualElement choice = LobbyUi.Box("charwin__seg");
            choice.AddToClassList("charwin__seg--wide");
            choice.Add(Radio("charwin-fallback-assistant", JobFallback.Assistant, "become " + _fallbackJob.ToLowerInvariant(),
                "The default: spawn as " + _fallbackJob));
            choice.Add(Radio("charwin-fallback-lobby", JobFallback.ReturnToLobby, "return to lobby", "Then observe, or join late"));
            _fallbackBar.Add(choice);
        }

        private Button Radio(string name, JobFallback value, string label, string hint)
        {
            Button radio = new(() =>
            {
                _model.SetFallback(value);
                Refresh();
                Changed?.Invoke();
            }) { name = name, userData = value, text = label, tooltip = hint };
            radio.AddToClassList("charwin__seg-option");
            radio.AddToClassList("charwin-jobs__radio");
            return radio;
        }

        /// <summary>Repaints rows, the notice and the fallback from the working copy, without rebuilding.</summary>
        public void Refresh()
        {
            if (_model.Working == null) return;
            foreach ((string job, VisualElement row) in _rows)
            {
                JobPriority current = _model.PriorityOf(job);
                row.EnableInClassList("charwin-jobs__row--set", current != JobPriority.Never);
                row.EnableInClassList("charwin-jobs__row--high", current == JobPriority.High);
                row.Q<Button>("charwin-job-about")?.EnableInClassList("charwin-jobs__about--open", _expanded.Contains(job));
                Label description = row.Q<Label>("charwin-job-description");
                description.style.display = _expanded.Contains(job) && description.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                VisualElement group = row.Q("charwin-job-priority");
                if (group == null) continue;
                foreach (JobPriority priority in Priorities)
                {
                    Button option = group.Q<Button>("charwin-prio-" + Key(priority));
                    option.EnableInClassList("charwin__seg-option--on", priority == current);
                    foreach (JobPriority each in Priorities)
                    {
                        option.EnableInClassList($"charwin-jobs__prio-option--{Key(each)}-on", each == priority && priority == current);
                    }
                }
            }

            {
                JobsNoticeKind kind = _model.Notice;
                string text = _model.NoticeText(_fallbackJob);
                // A notice that appears, or says something new, eases in again so the change is seen.
                bool news = kind != JobsNoticeKind.None
                    && (_notice.ClassListContains("charwin__notice--hidden") || text != _noticeText.text);
                _noticeText.text = text;
                _notice.EnableInClassList("charwin__notice--hidden", kind == JobsNoticeKind.None);
                if (news) CharacterWindowView.Enter(_notice);
                _notice.EnableInClassList("charwin__notice--success", kind == JobsNoticeKind.AllNever);
                _notice.Q(className: "lobby__icon").EnableInClassList("lobby__icon--check", kind == JobsNoticeKind.AllNever);
                _notice.Q(className: "lobby__icon").EnableInClassList("lobby__icon--info", kind != JobsNoticeKind.AllNever);
            }

            JobFallback fallback = _model.Working.Preferences.Fallback;
            foreach (Button radio in _fallbackBar.Query<Button>().ToList())
            {
                bool on = radio.userData is JobFallback value && value == fallback;
                radio.EnableInClassList("charwin-jobs__radio--on", on);
                radio.EnableInClassList("charwin__seg-option--on", on);
            }
        }

        private static string Key(JobPriority priority) => priority.ToString().ToLowerInvariant();
    }
}
