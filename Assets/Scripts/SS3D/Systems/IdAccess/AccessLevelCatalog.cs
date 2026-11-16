using System.Collections.Generic;

namespace SS3D.Systems.IdAccess
{
    public readonly struct AccessLevelEntry
    {
        public AccessLevel Level { get; }

        public string DisplayName { get; }

        public bool IsDepartment { get; }

        public AccessLevelEntry(AccessLevel level, string displayName, bool isDepartment)
        {
            Level = level;
            DisplayName = displayName;
            IsDepartment = isDepartment;
        }
    }

    public static class AccessLevelCatalog
    {
        private static readonly AccessLevelEntry[] EditableLevels =
        {
            new(AccessLevel.Command, "Command", true),
            new(AccessLevel.Security, "Security", true),
            new(AccessLevel.Engineering, "Engineering", true),
            new(AccessLevel.Medical, "Medical", true),
            new(AccessLevel.Science, "Science", true),
            new(AccessLevel.Cargo, "Cargo", true),
            new(AccessLevel.Service, "Service", true),
            new(AccessLevel.Civilian, "Civilian", true),
            new(AccessLevel.Bridge, "Bridge", false),
            new(AccessLevel.CaptainsOffice, "Captain's Office", false),
            new(AccessLevel.ChangeId, "Change ID", false),
            new(AccessLevel.EVA, "EVA", false),
            new(AccessLevel.Armory, "Armory", false),
            new(AccessLevel.EvidenceLockup, "Evidence Lockup", false),
            new(AccessLevel.AIUpload, "AI Upload", false),
            new(AccessLevel.Maintenance, "Maintenance", false),
            new(AccessLevel.Crew, "Crew", false),
        };

        public static IReadOnlyList<AccessLevelEntry> AllEditableLevels => EditableLevels;

        public static bool TryGetEntry(byte index, out AccessLevelEntry entry)
        {
            if (index >= EditableLevels.Length)
            {
                entry = default;
                return false;
            }

            entry = EditableLevels[index];
            return true;
        }
    }
}
