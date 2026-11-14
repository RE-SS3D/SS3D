using System.Collections.Generic;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Roles;

namespace SS3D.UI.Lobby
{
    /// <summary>What the Jobs tab's notice line says (lobby.md §3; the canvas's Jobs boards).</summary>
    public enum JobsNoticeKind
    {
        None = 0,
        /// <summary>A new High dropped the old High to Medium.</summary>
        HighMoved = 1,
        /// <summary>Every job is at Never: a valid choice, explained.</summary>
        AllNever = 2,
    }

    /// <summary>
    /// The character window's state (lobby effort Phase 5): the saved character and a working copy that the three tabs
    /// edit. One Save covers all three (Decision 5). No UI here, so the rules are unit-tested.
    /// </summary>
    public sealed class CharacterWindowModel
    {
        private string _movedFrom;
        private string _movedTo;

        /// <summary>The character as last saved; the roster holds this instance.</summary>
        public SavedCharacter Saved { get; private set; }

        /// <summary>The copy the window edits. Discard throws it away; Save stores it.</summary>
        public SavedCharacter Working { get; private set; }

        public bool IsDirty => Working != null && !Working.SameAs(Saved);

        public CharacterNameError NameError => CharacterNameRules.Validate(Working?.Name);

        public bool NameValid => NameError == CharacterNameError.None;

        /// <summary>Save needs a change and a valid name; while the name is invalid the reason is the tooltip.</summary>
        public bool CanSave => IsDirty && NameValid;

        public string SaveTooltip
        {
            get
            {
                if (!NameValid) return CharacterNameRules.Describe(NameError);
                return IsDirty ? "Save this character (Ctrl+S)" : "Nothing to save";
            }
        }

        public void Load(SavedCharacter saved)
        {
            Saved = saved;
            Working = saved?.Clone(saved.Id);
            ClearNotice();
        }

        /// <summary>
        /// Stores the working copy with its name normalised and returns it; the caller puts it in the roster. Null
        /// when Save isn't allowed.
        /// </summary>
        public SavedCharacter Commit()
        {
            if (!CanSave)
            {
                return null;
            }

            SavedCharacter saved = Working.Clone(Working.Id);
            saved.Name = CharacterNameRules.Normalize(Working.Name);
            Load(saved);
            return saved;
        }

        public void Discard() => Load(Saved);

        // ---- Jobs ----

        public JobPriority PriorityOf(string job) => Working.Preferences.PriorityOf(job);

        /// <summary>Sets a priority under the one-High rule; a High that displaces another raises the notice.</summary>
        public void SetPriority(string job, JobPriority priority)
        {
            string oldHigh = Working.Preferences.HighJob;
            Working.Preferences.SetPriority(job, priority);
            if (priority == JobPriority.High && oldHigh != null && oldHigh != job)
            {
                _movedTo = job;
                _movedFrom = oldHigh;
            }
            else
            {
                ClearNotice();
            }
        }

        public void SetFallback(JobFallback fallback) => Working.Preferences.Fallback = fallback;

        public JobsNoticeKind Notice
        {
            get
            {
                if (_movedTo != null) return JobsNoticeKind.HighMoved;
                return Working != null && Working.Preferences.ToDictionary().Count == 0 ? JobsNoticeKind.AllNever : JobsNoticeKind.None;
            }
        }

        /// <summary>The job that moved down when the last High was set, for the row's flash.</summary>
        public string MovedJob => _movedFrom;

        /// <summary>The notice line, as rich text (job names bold).</summary>
        public string NoticeText(string fallbackJob) => Notice switch
        {
            JobsNoticeKind.HighMoved =>
                $"<b>{_movedTo}</b> is now High. <b>{_movedFrom}</b> moved from High to Medium: only one job can be High.",
            JobsNoticeKind.AllNever => Working.Preferences.Fallback == JobFallback.ReturnToLobby
                ? "Every job is set to Never. That's a valid choice: you'll stay in the lobby when the round starts, and can observe or join late."
                : $"Every job is set to Never. That's a valid choice: you'll join as {fallbackJob} when the round starts.",
            _ => string.Empty,
        };

        private void ClearNotice()
        {
            _movedFrom = null;
            _movedTo = null;
        }

        // ---- Preview outfit (Decision 14) ----

        /// <summary>The job whose clothes the preview wears: the High job, else the fallback job.</summary>
        public string OutfitJob(string fallbackJob) => Working?.Preferences.HighJob ?? fallbackJob;

        public string OutfitCaption(string fallbackJob, bool showClothing)
        {
            if (!showClothing)
            {
                return "Clothing hidden. Other players see you in your job's clothes.";
            }

            string high = Working?.Preferences.HighJob;
            return high != null
                ? $"Wearing {high} clothes, from your High job."
                : $"Wearing {fallbackJob} clothes: nothing is set to High.";
        }

        // ---- Shared text ----

        /// <summary>"1 slot", "4 slots" or "unlimited": before the round only totals show (Decision 12).</summary>
        public static string SlotsText(JobSlotInfo job)
        {
            if (job.Unlimited) return "unlimited";
            return job.Slots == 1 ? "1 slot" : $"{job.Slots} slots";
        }

        /// <summary>"High: Job" or "No High job", the switcher's and the top bar's second line.</summary>
        public static string HighLine(SavedCharacter character) =>
            character?.Preferences?.HighJob is { } high ? "High: " + high : "No High job";

        /// <summary>The department of <paramref name="job"/> in <paramref name="jobs"/>, None when unknown.</summary>
        public static SS3D.Systems.IdAccess.Department DepartmentOf(string job, IEnumerable<JobSlotInfo> jobs)
        {
            if (job != null && jobs != null)
            {
                foreach (JobSlotInfo slot in jobs)
                {
                    if (slot.Name == job)
                    {
                        return (SS3D.Systems.IdAccess.Department)slot.Department;
                    }
                }
            }

            return SS3D.Systems.IdAccess.Department.None;
        }
    }
}
