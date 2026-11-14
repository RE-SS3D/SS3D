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
    /// The character window's Jobs tab (lobby.md §2–§3, lobby effort Phase 5): jobs by department, total slots only
    /// (Decision 12), a High / Medium / Low / Never radio group per job under the one-High rule, expandable
    /// descriptions, locked rows, the notice line and the pinned fallback choice. It edits
    /// <see cref="CharacterWindowModel.Working"/>.
    /// </summary>
    public sealed class CharacterJobsTab
    {
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
        private VisualElement _notice;
        private Label _noticeText;
        private bool _compact;

        public event Action Changed;

        public CharacterJobsTab(CharacterWindowModel model, LobbyAssetCatalog catalog)
        {
            _model = model;
            _catalog = catalog;
            Root = LobbyUi.Box("charwin-tab");
            Root.name = "charwin-jobs";
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("charwin-tab__scroll");
            Root.Add(_scroll);
            _fallbackBar = LobbyUi.Box("charwin-jobs__fallback");
            _fallbackBar.name = "charwin-fallback";
            Root.Add(_fallbackBar);
        }

        public VisualElement Root { get; }

        /// <summary>
        /// The server's jobs in authoring order, their descriptions, the playtime lock reason (null when open; no job
        /// has a requirement yet, see the effort's deviations) and the uncapped fallback job.
        /// </summary>
        public void SetJobs(IReadOnlyList<JobSlotInfo> jobs, Func<string, string> describe, Func<string, string> lockedReason, string fallbackJob)
        {
            _jobs = jobs ?? Array.Empty<JobSlotInfo>();
            _describe = describe ?? (_ => string.Empty);
            _lockedReason = lockedReason ?? (_ => null);
            _fallbackJob = string.IsNullOrEmpty(fallbackJob) ? "Assistant" : fallbackJob;
        }

        /// <summary>"Medium" shortens to "Med" in the 1366 layout.</summary>
        public void SetCompact(bool compact)
        {
            if (_compact == compact) return;
            _compact = compact;
            Refresh();
        }

        public void Rebuild()
        {
            _rows.Clear();
            _scroll.Clear();
            if (_model.Working == null) return;

            VisualElement top = LobbyUi.Box("charwin-jobs__top");
            top.Add(LobbyUi.Text("Rank the jobs you want. One job can be High; Medium and Low take as many as you like. "
                + "Everyone's preferences are resolved together when the round starts, so there is no rush.", "charwin-tab__intro"));
            VisualElement legend = LobbyUi.Box("charwin-jobs__legend");
            foreach (JobPriority priority in Priorities)
            {
                VisualElement item = LobbyUi.Box("charwin-jobs__legend-item");
                item.Add(LobbyUi.Box("charwin-jobs__legend-swatch", "charwin-jobs__legend-swatch--" + Key(priority)));
                item.Add(LobbyUi.Text(priority.ToString(), "charwin-jobs__legend-label"));
                legend.Add(item);
            }

            top.Add(legend);
            _scroll.Add(top);

            _notice = LobbyUi.Box("charwin__notice");
            _notice.name = "charwin-jobs-notice";
            _notice.Add(LobbyUi.Icon("info"));
            _noticeText = LobbyUi.Text("", "charwin__notice-text");
            _noticeText.enableRichText = true;
            _notice.Add(_noticeText);
            _scroll.Add(_notice);

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
            VisualElement head = LobbyUi.Box("charwin-jobs__dept-head");
            VisualElement icon = DepartmentIcon(department);
            icon.AddToClassList("charwin-jobs__dept-icon");
            head.Add(icon);
            Label name = LobbyUi.Titling(department == Department.None ? "Other" : department.ToString(), "charwin-jobs__dept-name");
            if (dept != null) name.AddToClassList(dept);
            head.Add(name);
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
            VisualElement main = LobbyUi.Box("charwin-jobs__row-main");

            VisualElement frame = LobbyUi.Box("charwin-jobs__job-icon-frame");
            frame.Add(JobIcon(name, department));
            main.Add(frame);
            main.Add(LobbyUi.Text(name, "charwin-jobs__job-name"));

            string description = _describe(name);
            Button about = new() { name = "charwin-job-about", tooltip = "About " + name };
            about.AddToClassList("charwin-jobs__about");
            about.Add(LobbyUi.Icon("chevron-right"));
            about.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;
            main.Add(about);
            main.Add(LobbyUi.Box("lobby__grow"));
            main.Add(LobbyUi.Mono(CharacterWindowModel.SlotsText(job), "charwin-jobs__slots"));

            string locked = _lockedReason(name);
            if (locked != null)
            {
                VisualElement lockBox = LobbyUi.Box("charwin-jobs__locked");
                lockBox.Add(LobbyUi.Icon("lock"));
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

        private VisualElement JobIcon(string job, Department department)
        {
            string id = IconAliases.TryGetValue(job, out string alias) ? alias : job.Replace(" ", "");
            VisualElement icon;
            if (_catalog != null && _catalog.TryGetJobIcon(id, out Sprite sprite) && sprite != null)
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

        /// <summary>A radio group: Tab reaches it, ←/→ change it, a click picks an option.</summary>
        private VisualElement BuildPriorityGroup(string job)
        {
            VisualElement group = LobbyUi.Box("charwin-jobs__prio");
            group.name = "charwin-job-priority";
            group.focusable = true;
            group.tooltip = "Priority for " + job + " (←/→)";
            for (int i = 0; i < Priorities.Length; i++)
            {
                JobPriority priority = Priorities[i];
                Button option = new(() => Set(job, priority)) { name = "charwin-prio-" + Key(priority), focusable = false };
                option.AddToClassList("charwin-jobs__prio-option");
                option.AddToClassList("lb-titling");
                option.EnableInClassList("charwin-jobs__prio-option--first", i == 0);
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
            _fallbackBar.Add(LobbyUi.Text("If none of my jobs is available:", "charwin-jobs__fallback-label"));
            _fallbackBar.Add(Radio("charwin-fallback-assistant", JobFallback.Assistant, "Become " + _fallbackJob, "Default"));
            _fallbackBar.Add(Radio("charwin-fallback-lobby", JobFallback.ReturnToLobby, "Return to lobby", "Then observe, or join late"));
        }

        private Button Radio(string name, JobFallback value, string label, string hint)
        {
            Button radio = new(() =>
            {
                _model.SetFallback(value);
                Refresh();
                Changed?.Invoke();
            }) { name = name, userData = value };
            radio.AddToClassList("charwin-jobs__radio");
            VisualElement ring = LobbyUi.Box("charwin-jobs__radio-ring");
            ring.Add(LobbyUi.Box("charwin-jobs__radio-dot"));
            radio.Add(ring);
            radio.Add(LobbyUi.Text(label, "charwin-jobs__radio-label"));
            radio.Add(LobbyUi.Text(hint, "charwin-jobs__radio-hint"));
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
                    option.text = (priority == JobPriority.Medium && _compact ? "Med" : priority.ToString()).ToUpperInvariant();
                    foreach (JobPriority each in Priorities)
                    {
                        option.EnableInClassList($"charwin-jobs__prio-option--{Key(each)}-on", each == priority && priority == current);
                    }
                }
            }

            if (_notice != null)
            {
                JobsNoticeKind kind = _model.Notice;
                _noticeText.text = _model.NoticeText(_fallbackJob);
                _notice.EnableInClassList("charwin__notice--hidden", kind == JobsNoticeKind.None);
                _notice.EnableInClassList("charwin__notice--success", kind == JobsNoticeKind.AllNever);
                _notice.Q(className: "lobby__icon").EnableInClassList("lobby__icon--check", kind == JobsNoticeKind.AllNever);
                _notice.Q(className: "lobby__icon").EnableInClassList("lobby__icon--info", kind != JobsNoticeKind.AllNever);
            }

            JobFallback fallback = _model.Working.Preferences.Fallback;
            foreach (Button radio in _fallbackBar.Query<Button>().ToList())
            {
                radio.EnableInClassList("charwin-jobs__radio--on", radio.userData is JobFallback value && value == fallback);
            }
        }

        private static string Key(JobPriority priority) => priority.ToString().ToLowerInvariant();
    }
}
