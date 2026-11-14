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

        /// <summary>
        /// Roster order and one-line descriptions (lobby.md §2: every job has one). A job with role data shows its
        /// <see cref="RoleData.Description"/> instead; these cover the jobs not in the game yet.
        /// </summary>
        public static readonly Entry[] All =
        {
            new("Captain", Department.Command, null,
                "Commands the station: sets priorities for every department and holds the highest access."),
            new("Head of Personnel", Department.Command, null,
                "Runs the crew: changes jobs and ID access at the ID console, and oversees Service and Cargo."),
            new("Head of Security", Department.Security, null,
                "Leads Security: assigns officers, keeps the armory and answers for the brig."),
            new("Security Officer", Department.Security, "Security",
                "Keeps the peace: patrols the station, stops crime and takes suspects to the brig."),
            new("Detective", Department.Security, null,
                "Investigates crimes: gathers evidence, examines scenes and builds the case for Security."),
            new("Chief Engineer", Department.Engineering, null,
                "Leads Engineering: keeps power, atmosphere and the hull running, and directs repairs."),
            new("Engineer", Department.Engineering, null,
                "Keeps the station running: sets up and repairs power, fixes breaches and builds what's needed."),
            new("Atmospheric Technician", Department.Engineering, null,
                "Keeps the air breathable: runs the pipes, scrubbers and vents, and refills what a breach drains."),
            new("Chief Medical Officer", Department.Medical, null,
                "Leads Medical: directs doctors and chemists and decides who gets treated first."),
            new("Doctor", Department.Medical, null,
                "Treats the injured: stops bleeding, mends wounds and brings people back from the brink."),
            new("Chemist", Department.Medical, null,
                "Makes medicine: mixes the chemicals Medical needs, and some that it doesn't."),
            new("Research Director", Department.Science, null,
                "Leads Science: sets what gets researched and guards the station's most dangerous experiments."),
            new("Scientist", Department.Science, null,
                "Researches new technology: runs experiments and turns the results into new equipment."),
            new("Quartermaster", Department.Cargo, null,
                "Runs Cargo: approves orders from other departments, spends the station budget and decides what comes up on the supply shuttle."),
            new("Cargo Technician", Department.Cargo, null,
                "Moves supplies: unloads the shuttle, fills orders and delivers crates across the station."),
            new("Bartender", Department.Service, null,
                "Runs the bar: mixes drinks and keeps the crew's one place to unwind in order."),
            new("Chef", Department.Service, null,
                "Feeds the crew: cooks meals in the kitchen from what Cargo and the station provide."),
            new("Janitor", Department.Service, null,
                "Keeps the station clean: mops spills, clears debris and replaces broken lights."),
            new("Assistant", Department.Civilian, null,
                "Helps wherever needed: no department and little access, but free to lend a hand anywhere."),
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
