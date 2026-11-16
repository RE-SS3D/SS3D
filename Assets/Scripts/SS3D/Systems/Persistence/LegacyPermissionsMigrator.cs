using SS3D.Data;
using SS3D.Data.Persistence;
using SS3D.Permissions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SS3D.Systems.Persistence
{
    public static class LegacyPermissionsMigrator
    {
        private const string ConfigFileName = "permissions.txt";

        private static readonly string PermissionsPath = Paths.GetPath(GamePaths.Config, true) + "/" + ConfigFileName;

        /// <summary>
        /// Reads Config/permissions.txt, or the tracked permissions.default.txt beside it when the live file
        /// doesn't exist yet (a fresh checkout or install: the live file is gitignored).
        /// </summary>
        public static bool TryLoadFromLegacyTxt(out SavedPermissionsPayload payload)
        {
            if (TryLoadFromLegacyTxtAtPath(PermissionsPath, out payload))
                return true;

            string defaults = Path.Combine(Path.GetDirectoryName(PermissionsPath) ?? "", PermissionSubSystem.DefaultConfigFileName);
            return TryLoadFromLegacyTxtAtPath(defaults, out payload);
        }

        public static bool TryLoadFromLegacyTxtAtPath(string path, out SavedPermissionsPayload payload)
        {
            payload = null;

            if (!File.Exists(path))
            {
                return false;
            }

            var records = new List<SavedPermissionRecord>();
            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 2)
                {
                    continue;
                }

                records.Add(new SavedPermissionRecord
                {
                    ckey = words[0],
                    role = words[1],
                });
            }

            payload = new SavedPermissionsPayload { records = records.ToArray() };
            return true;
        }

        public static SavedPermissionRecord[] ToRecords(IEnumerable<KeyValuePair<string, ServerRoleTypes>> permissions)
        {
            var records = new List<SavedPermissionRecord>();
            foreach (KeyValuePair<string, ServerRoleTypes> permission in permissions)
            {
                records.Add(new SavedPermissionRecord
                {
                    ckey = permission.Key,
                    role = permission.Value.ToString(),
                });
            }

            return records.ToArray();
        }
    }
}
