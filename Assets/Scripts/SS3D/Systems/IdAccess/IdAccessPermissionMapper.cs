using System.Collections.Generic;
using SS3D.Systems.Roles;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Temporary bridge from legacy IDPermission trait assets to access bitmasks.
    /// Remove once all consumers use AccessMask directly.
    /// </summary>
    public static class IdAccessPermissionMapper
    {
        private static readonly Dictionary<string, AccessMask> MasksByName = new()
        {
            ["General"] = AccessPresets.StandardCrew,
            ["Security"] = AccessPresets.SecurityOfficer,
        };

        public static AccessMask FromLegacyPermission(IDPermission permission)
        {
            if (permission == null)
            {
                return AccessMask.None;
            }

            return MasksByName.TryGetValue(permission.Name, out AccessMask mask)
                ? mask
                : AccessMask.None;
        }

        public static AccessMask FromLegacyPermissions(IEnumerable<IDPermission> permissions)
        {
            AccessMask combined = AccessMask.None;
            if (permissions == null)
            {
                return combined;
            }

            foreach (IDPermission permission in permissions)
            {
                combined |= FromLegacyPermission(permission);
            }

            return combined;
        }
    }

    public static class AccessPresets
    {
        public static readonly AccessMask StandardCrew = AccessMask.FromLevels(
            AccessLevel.Civilian,
            AccessLevel.Crew,
            AccessLevel.Maintenance);

        public static readonly AccessMask SecurityOfficer = AccessMask.FromLevels(
            AccessLevel.Security,
            AccessLevel.Armory,
            AccessLevel.EvidenceLockup,
            AccessLevel.Civilian,
            AccessLevel.Crew,
            AccessLevel.Maintenance);

        public static readonly AccessMask HeadOfPersonnel = AccessMask.FromLevels(
            AccessLevel.Command,
            AccessLevel.ChangeId,
            AccessLevel.EVA,
            AccessLevel.Crew,
            AccessLevel.Maintenance);

        public static readonly AccessMask Engineer = AccessMask.FromLevels(
            AccessLevel.Engineering,
            AccessLevel.EVA,
            AccessLevel.Crew,
            AccessLevel.Maintenance);

        public static readonly AccessMask Captain = AccessMask.FromLevels(
            AccessLevel.Command,
            AccessLevel.Security,
            AccessLevel.Engineering,
            AccessLevel.Medical,
            AccessLevel.Science,
            AccessLevel.Cargo,
            AccessLevel.Service,
            AccessLevel.Civilian,
            AccessLevel.Bridge,
            AccessLevel.CaptainsOffice,
            AccessLevel.ChangeId,
            AccessLevel.EVA,
            AccessLevel.Crew,
            AccessLevel.Maintenance);
    }
}
