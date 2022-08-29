using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;

namespace SS3D.Systems.Permissions
{
    /// <summary>
    /// TODO: Make a simple permission system based on a .txt file to avoid non-admins from starting a round
    /// </summary>
    public class PermissionSystem : NetworkBehaviour
    {
        private const string EditorPermissionFilePath = "/Builds/Config/permissions.txt";
        private const string PermissionFilePath = "/Config/permissions.txt";

        private static string FullPermissionFilePath => Path.GetFullPath(".") + (Application.isEditor ? EditorPermissionFilePath : PermissionFilePath);

        private readonly Dictionary<string, ServerRoleTypes> _userPermissions = new();

        [Server]
        public ServerRoleTypes GetUserPermission(string ckey)
        {
            bool containsKey = _userPermissions.ContainsKey(ckey);

            return containsKey ? _userPermissions[ckey] : ServerRoleTypes.User;
        }

        [Server]
        public void ChangeUserPermission(string ckey, ServerRoleTypes role)
        {
            // TODO: This
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            LoadPermissions();
        }

        [Server]
        private void LoadPermissions()
        {
            CreatePermissionsFileIfNotExists();

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

        private static void CreatePermissionsFileIfNotExists()
        {
            if (!File.Exists(FullPermissionFilePath))
            {
                Debug.Log($"[{nameof(PermissionSystem)}] - Permissions file not found, creating a new one");
                File.WriteAllText(FullPermissionFilePath, string.Empty);
            }
        }
    }
}
