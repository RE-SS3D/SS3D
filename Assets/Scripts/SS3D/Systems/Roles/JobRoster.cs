using System.Collections.Generic;
using SS3D.Systems.IdAccess;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// The station's full job roster, in department order, from the lobby canvas's Jobs board (the jobs `id-access.md`
    /// §5 lists). The character window shows every one: a job this server runs is a normal row, and one without role
    /// data yet is a locked row (lobby.md §2: locked jobs stay visible, not hidden).
    /// </summary>
    public static class JobRoster
    {
        public readonly struct Entry
        {
            public readonly string Name;
            public readonly Department Department;
            /// <summary>The role-data name of the same job when it differs ("Security" for Security Officer), or null.</summary>
            public readonly string GameName;
            public readonly string Description;

            public Entry(string name, Department department, string gameName = null, string description = "")
            {
                Name = name;
                Department = department;
                GameName = gameName;
                Description = description;
            }

            /// <summary>The name the server's job list uses for this job.</summary>
            public string Key => GameName ?? Name;
        }

        public static readonly Entry[] All =
        {
            new("Captain", Department.Command),
            new("Head of Personnel", Department.Command),
            new("Head of Security", Department.Security),
            new("Security Officer", Department.Security, "Security"),
            new("Detective", Department.Security),
            new("Chief Engineer", Department.Engineering),
            new("Engineer", Department.Engineering),
            new("Atmospheric Technician", Department.Engineering),
            new("Chief Medical Officer", Department.Medical),
            new("Doctor", Department.Medical),
            new("Chemist", Department.Medical),
            new("Research Director", Department.Science),
            new("Scientist", Department.Science),
            new("Quartermaster", Department.Cargo, null,
                "Runs Cargo: approves orders from other departments, spends the station budget and decides what comes up on the supply shuttle."),
            new("Cargo Technician", Department.Cargo),
            new("Bartender", Department.Service),
            new("Chef", Department.Service),
            new("Janitor", Department.Service),
            new("Assistant", Department.Civilian),
        };

        /// <summary>The roster's description of <paramref name="job"/> (its key), or empty.</summary>
        public static string DescriptionOf(string job)
        {
            foreach (Entry entry in All)
            {
                if (entry.Key == job) return entry.Description;
            }

            return string.Empty;
        }

        /// <summary>
        /// The server's jobs merged into the roster: every roster job in order, the server's entry where it has one,
        /// and any server job the roster lacks after its department's roster jobs. <paramref name="locked"/> gets the
        /// roster jobs this server doesn't run.
        /// </summary>
        public static List<JobSlotInfo> Merge(IReadOnlyList<JobSlotInfo> serverJobs, HashSet<string> locked)
        {
            Dictionary<string, JobSlotInfo> byName = new();
            foreach (JobSlotInfo job in serverJobs) byName[job.Name] = job;
            HashSet<string> placed = new();
            List<JobSlotInfo> merged = new();
            foreach (Department department in System.Enum.GetValues(typeof(Department)))
            {
                foreach (Entry entry in All)
                {
                    if (entry.Department != department) continue;
                    if (byName.TryGetValue(entry.Key, out JobSlotInfo job))
                    {
                        merged.Add(job);
                        placed.Add(entry.Key);
                    }
                    else
                    {
                        merged.Add(new JobSlotInfo { Name = entry.Key, Department = (byte)department });
                        locked?.Add(entry.Key);
                    }
                }

                foreach (JobSlotInfo job in serverJobs)
                {
                    if ((Department)job.Department == department && !placed.Contains(job.Name))
                    {
                        merged.Add(job);
                        placed.Add(job.Name);
                    }
                }
            }

            return merged;
        }
    }
}
