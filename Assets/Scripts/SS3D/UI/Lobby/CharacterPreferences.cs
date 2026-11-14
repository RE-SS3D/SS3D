using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Roles;
using SS3D.Systems.Roles.Messages;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// A saved character's job priorities, antagonist choices and fallback (lobby.md §3–§4, lobby effort Decision 1).
    /// Stored in <c>characters.json</c> as parallel arrays because <c>JsonUtility</c> can't serialize dictionaries.
    /// A job missing from <see cref="Jobs"/> is Never; an antagonist category in neither list is unset and takes the
    /// server's default (off, except on the <c>-nukeops</c> profile).
    /// </summary>
    [Serializable]
    public sealed class CharacterPreferences
    {
        public string[] Jobs = Array.Empty<string>();
        public byte[] Priorities = Array.Empty<byte>();
        public string[] AntagonistOptIns = Array.Empty<string>();
        public string[] AntagonistOptOuts = Array.Empty<string>();
        public JobFallback Fallback = JobFallback.Assistant;

        /// <summary>The arrays line up and hold only known values (a check on loaded files).</summary>
        public bool IsWellFormed =>
            Jobs != null && Priorities != null && Jobs.Length == Priorities.Length
            && Priorities.All(priority => priority <= (byte)JobPriority.High)
            && AntagonistOptIns != null && AntagonistOptOuts != null
            && Enum.IsDefined(typeof(JobFallback), Fallback);

        public JobPriority PriorityOf(string job)
        {
            int index = Array.IndexOf(Jobs, job);
            return index >= 0 ? (JobPriority)Priorities[index] : JobPriority.Never;
        }

        /// <summary>
        /// Sets one job's priority. Only one job can be High (lobby.md §3): setting a new High drops the old one to
        /// Medium.
        /// </summary>
        public void SetPriority(string job, JobPriority priority)
        {
            if (string.IsNullOrEmpty(job))
            {
                return;
            }

            Dictionary<string, JobPriority> priorities = ToDictionary();
            if (priority == JobPriority.High)
            {
                foreach (string other in priorities.Keys.ToArray())
                {
                    if (other != job && priorities[other] == JobPriority.High)
                    {
                        priorities[other] = JobPriority.Medium;
                    }
                }
            }

            if (priority == JobPriority.Never)
            {
                priorities.Remove(job);
            }
            else
            {
                priorities[job] = priority;
            }

            Jobs = priorities.Keys.ToArray();
            Priorities = Jobs.Select(key => (byte)priorities[key]).ToArray();
        }

        /// <summary>Every job set above Never.</summary>
        public Dictionary<string, JobPriority> ToDictionary()
        {
            Dictionary<string, JobPriority> result = new();
            for (int i = 0; i < Math.Min(Jobs.Length, Priorities.Length); i++)
            {
                if (Jobs[i] != null && Priorities[i] > (byte)JobPriority.Never)
                {
                    result[Jobs[i]] = (JobPriority)Priorities[i];
                }
            }

            return result;
        }

        /// <summary>True or false once the character chose; null while the category takes the server default.</summary>
        public bool? AntagonistChoice(string category)
        {
            if (AntagonistOptIns.Contains(category))
            {
                return true;
            }

            return AntagonistOptOuts.Contains(category) ? false : null;
        }

        /// <summary>What the category shows and does on a server whose default is <paramref name="serverDefault"/>.</summary>
        public bool IsAntagonistOn(string category, bool serverDefault) =>
            AntagonistCategories.IsOptedIn(AntagonistOptIns, AntagonistOptOuts, category, serverDefault);

        public void SetAntagonist(string category, bool optedIn)
        {
            AntagonistOptIns = AntagonistOptIns.Where(key => key != category).ToArray();
            AntagonistOptOuts = AntagonistOptOuts.Where(key => key != category).ToArray();
            if (optedIn)
            {
                AntagonistOptIns = AntagonistOptIns.Append(category).ToArray();
            }
            else
            {
                AntagonistOptOuts = AntagonistOptOuts.Append(category).ToArray();
            }
        }

        public CharacterPreferences Clone() => new()
        {
            Jobs = (string[])Jobs.Clone(),
            Priorities = (byte[])Priorities.Clone(),
            AntagonistOptIns = (string[])AntagonistOptIns.Clone(),
            AntagonistOptOuts = (string[])AntagonistOptOuts.Clone(),
            Fallback = Fallback,
        };

        /// <summary>The message the client sends for the selected character (lobby effort Decision 6).</summary>
        public JobPreferencesMessage ToMessage()
        {
            Dictionary<string, JobPriority> priorities = ToDictionary();
            string[] jobs = priorities.Keys.ToArray();
            return new JobPreferencesMessage
            {
                Jobs = jobs,
                Priorities = jobs.Select(job => (byte)priorities[job]).ToArray(),
                AntagonistOptIns = (string[])AntagonistOptIns.Clone(),
                AntagonistOptOuts = (string[])AntagonistOptOuts.Clone(),
                Fallback = (byte)Fallback,
            };
        }
    }
}
