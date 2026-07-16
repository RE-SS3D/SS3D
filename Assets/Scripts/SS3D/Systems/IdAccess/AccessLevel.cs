using System;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Named access levels represented as a flat bitmask. Department and cross-cutting levels
    /// share the same flag space per design/id-access.md §4.
    /// </summary>
    [Flags]
    public enum AccessLevel : ulong
    {
        None = 0,

        // Department levels
        Command = 1UL << 0,
        Security = 1UL << 1,
        Engineering = 1UL << 2,
        Medical = 1UL << 3,
        Science = 1UL << 4,
        Cargo = 1UL << 5,
        Service = 1UL << 6,
        Civilian = 1UL << 7,

        // Cross-cutting levels
        Bridge = 1UL << 8,
        CaptainsOffice = 1UL << 9,
        ChangeId = 1UL << 10,
        EVA = 1UL << 11,
        Armory = 1UL << 12,
        EvidenceLockup = 1UL << 13,
        AIUpload = 1UL << 14,
        Maintenance = 1UL << 15,
        Crew = 1UL << 16,
    }
}
