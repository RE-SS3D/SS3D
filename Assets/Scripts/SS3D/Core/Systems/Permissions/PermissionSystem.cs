using System;
using System.Collections.Generic;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;

namespace SS3D.Core.Systems.Permissions
{
    /// <summary>
    /// TODO: Make a simple permission system based on a .txt file to avoid non-admins from starting a round
    /// </summary>
    public class PermissionSystem : MonoBehaviour
    {
        private const string PermissionFilePath = "/Builds/Config/permissions.txt";

        private static string FullPermissionFilePath => Path.GetFullPath(".") + PermissionFilePath;

        private readonly Dictionary<string, ServerRoleTypes> _userPermissions = new();

        public ServerRoleTypes GetUserPermission(string ckey)
        {
            bool containsKey = _userPermissions.ContainsKey(ckey);

            return containsKey ? _userPermissions[ckey] : ServerRoleTypes.User;
        }

        private void Start()
        {
            LoadPermissions();
        }

        private void LoadPermissions()
        {
            string[] lines = File.ReadAllLines(FullPermissionFilePath);

            foreach (string line in lines)
            {
                string[] words = line.Split(" ");

                string ckey = words[0];
                Enum.TryParse(words[1], out ServerRoleTypes role);

                _userPermissions.Add(ckey, role);

                Debug.Log($"[{nameof(PermissionSystem)}] - Found user permission {ckey} as {role}");
            }
        }
    }
}
