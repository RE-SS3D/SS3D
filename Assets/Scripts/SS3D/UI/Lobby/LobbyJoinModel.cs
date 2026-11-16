using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;

namespace SS3D.UI.Lobby
{
    /// <summary>One crew manifest group: a department's crew, plus a row for each open job nobody holds yet.</summary>
    public sealed class JoinManifestGroup
    {
        public Department Department;
        public readonly List<CrewManifestEntry> Crew = new();
        /// <summary>Open jobs in this department that nobody aboard holds ("No Doctor aboard").</summary>
        public readonly List<string> Missing = new();

        public int Rows => Crew.Count + Missing.Count;
    }

    /// <summary>What the notice line above the open jobs says.</summary>
    public enum JoinNoticeKind
    {
        None = 0,
        /// <summary>Every capped job is full; only the fallback job is open (canvas board Join-AssistantOnly).</summary>
        OnlyFallback = 1,
        /// <summary>The chosen job filled while the player was choosing; the pick moved.</summary>
        ChosenFilled = 2,
    }

    /// <summary>
    /// Rules for the Join picker (lobby.md §7, lobby effort Phase 6), with no UI: which jobs are open and in what order,
    /// their open/total text, which job is chosen and how the choice moves when that job fills, the notice line, and the
    /// crew manifest grouped by department. <see cref="LobbyJoinPicker"/> paints it.
    /// </summary>
    public sealed class LobbyJoinModel
    {
        private static readonly JobPriority[] Ranked = { JobPriority.High, JobPriority.Medium, JobPriority.Low };

        private readonly List<JobSlotInfo> _open = new();
        private IReadOnlyList<JobSlotInfo> _jobs = Array.Empty<JobSlotInfo>();
        private CharacterPreferences _preferences;
        private bool _picked;

        /// <summary>The jobs a latejoiner can take now, in roster order; the fallback job is always among them.</summary>
        public IReadOnlyList<JobSlotInfo> OpenJobs => _open;

        public string FallbackJob { get; private set; } = "Assistant";

        /// <summary>The job Join will ask for, or null when nothing is open (no job list yet).</summary>
        public string Selected { get; private set; }

        /// <summary>The job that filled while it was chosen, until the player picks again.</summary>
        public string FilledJob { get; private set; }

        /// <summary>
        /// Takes the replicated job list and the selected character's preferences. A job is open when it has a free slot
        /// or is the fallback job (no cap, Decision 7). The first time, and whenever the player hasn't picked, the choice
        /// is the character's best-ranked open job, else the first open job. A chosen job that fills moves the choice
        /// the same way and raises <see cref="JoinNoticeKind.ChosenFilled"/>.
        /// </summary>
        public void Set(IReadOnlyList<JobSlotInfo> jobs, string fallbackJob, CharacterPreferences preferences)
        {
            _jobs = jobs ?? Array.Empty<JobSlotInfo>();
            FallbackJob = string.IsNullOrEmpty(fallbackJob) ? "Assistant" : fallbackJob;
            bool preferencesChanged = !ReferenceEquals(preferences, _preferences);
            _preferences = preferences;

            _open.Clear();
            HashSet<string> notRun = new();
            foreach (JobSlotInfo job in JobRoster.Merge(_jobs, notRun))
            {
                if (notRun.Contains(job.Name)) continue;
                if (job.Name == FallbackJob || job.Open > 0) _open.Add(job);
            }

            bool stillOpen = Selected != null && IsOpen(Selected);
            if (Selected != null && !stillOpen)
            {
                FilledJob = Selected;
                _picked = false;
            }

            if (!_picked && (!stillOpen || preferencesChanged)) Selected = DefaultChoice();
        }

        /// <summary>The player picked <paramref name="job"/>; ignored unless it is open.</summary>
        public bool Select(string job)
        {
            if (!IsOpen(job)) return false;
            Selected = job;
            _picked = true;
            FilledJob = null;
            return true;
        }

        /// <summary>The picker opened: the choice starts over from the character's preferences.</summary>
        public void Reset()
        {
            _picked = false;
            FilledJob = null;
            Selected = DefaultChoice();
        }

        public bool IsOpen(string job) => job != null && _open.Any(open => open.Name == job);

        private string DefaultChoice()
        {
            if (_preferences != null)
                foreach (JobPriority priority in Ranked)
                    foreach (JobSlotInfo job in _open)
                        if (_preferences.PriorityOf(job.Name) == priority) return job.Name;
            return _open.Count > 0 ? _open[0].Name : null;
        }

        /// <summary>Only the uncapped fallback is open, though the station runs other jobs.</summary>
        public bool OnlyFallbackOpen => _open.Count == 1 && _open[0].Name == FallbackJob && _jobs.Any(job => job.Name != FallbackJob);

        public JoinNoticeKind Notice
        {
            get
            {
                if (FilledJob != null) return JoinNoticeKind.ChosenFilled;
                return OnlyFallbackOpen ? JoinNoticeKind.OnlyFallback : JoinNoticeKind.None;
            }
        }

        public string NoticeText => Notice switch
        {
            JoinNoticeKind.ChosenFilled => OnlyFallbackOpen
                ? $"{FilledJob} filled up while you were choosing, and so did every other job. {FallbackJob} is always open, or you can observe instead."
                : $"{FilledJob} filled up while you were choosing. Pick another job.",
            JoinNoticeKind.OnlyFallback =>
                $"Every other job is full right now. {FallbackJob} is always open, or you can observe instead.",
            _ => "",
        };

        /// <summary>"2 open".</summary>
        public string OpenCountText => $"{_open.Count} open";

        /// <summary>Open and total slots ("3/3"), or empty for an uncapped job, which reads "unlimited".</summary>
        public string CountText(JobSlotInfo job) =>
            job.Unlimited || job.Name == FallbackJob ? "" : $"{job.Open}/{job.Slots}";

        public string JoinTitle => Selected == null ? "Join" : "Join as " + Selected;

        /// <summary>
        /// The crew manifest by department, in department order (Other last). Crew rows follow the roster's job order.
        /// Each open job nobody holds adds a "No X aboard" row to its department, so the gap a latejoiner could fill shows.
        /// </summary>
        public List<JoinManifestGroup> Manifest(IReadOnlyList<CrewManifestEntry> manifest)
        {
            Dictionary<Department, JoinManifestGroup> groups = new();
            JoinManifestGroup GroupOf(Department department)
            {
                if (groups.TryGetValue(department, out JoinManifestGroup group)) return group;
                group = new JoinManifestGroup { Department = department };
                groups[department] = group;
                return group;
            }

            List<CrewManifestEntry> crew = (manifest ?? Array.Empty<CrewManifestEntry>()).ToList();
            foreach (CrewManifestEntry entry in crew.OrderBy(entry => RosterIndex(entry.Job)))
                GroupOf(DepartmentOf(entry.Job)).Crew.Add(entry);

            foreach (JobSlotInfo job in _open)
            {
                if (job.Name == FallbackJob || crew.Any(entry => entry.Job == job.Name)) continue;
                GroupOf(DepartmentOf(job.Name)).Missing.Add(job.Name);
            }

            return groups.Values.OrderBy(group => group.Department == Department.None ? int.MaxValue : (int)group.Department).ToList();
        }

        /// <summary>The department a job belongs to: the server's list, then the roster; Other when neither knows it.</summary>
        public Department DepartmentOf(string job)
        {
            foreach (JobSlotInfo slot in _jobs)
                if (slot.Name == job) return (Department)slot.Department;
            foreach (JobRoster.Entry entry in JobRoster.All)
                if (entry.Key == job) return entry.Department;
            return Department.None;
        }

        private static int RosterIndex(string job)
        {
            for (int i = 0; i < JobRoster.All.Length; i++)
                if (JobRoster.All[i].Key == job) return i;
            return JobRoster.All.Length;
        }

        /// <summary>
        /// Two columns of about equal rows, departments kept in order: the first column takes groups until it holds half
        /// the rows (the canvas's Join boards).
        /// </summary>
        public static List<List<JoinManifestGroup>> SplitColumns(IReadOnlyList<JoinManifestGroup> groups)
        {
            int total = groups.Sum(group => group.Rows + 1);
            List<List<JoinManifestGroup>> columns = new() { new List<JoinManifestGroup>(), new List<JoinManifestGroup>() };
            int left = 0;
            bool spilled = false;
            foreach (JoinManifestGroup group in groups)
            {
                spilled |= left * 2 >= total;
                columns[spilled ? 1 : 0].Add(group);
                if (!spilled) left += group.Rows + 1;
            }

            return columns;
        }
    }
}
