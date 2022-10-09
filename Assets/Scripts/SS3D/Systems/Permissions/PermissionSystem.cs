using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Permissions.Events;
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

        [SyncVar(OnChange = "SyncUserPermissions")]
        private Dictionary<string, ServerRoleTypes> _userPermissions = new();

        [Server]
        public ServerRoleTypes GetUserPermission(string ckey)
        {
            if (_userPermissions.Count == 0)
            {
                LoadPermissions();
            }

            bool containsKey = _userPermissions.ContainsKey(ckey);

            return containsKey ? _userPermissions[ckey] : ServerRoleTypes.User;
        }

        public bool CanUserPerformAction(ServerRoleTypes requiredRole, string ckey)
        {
            return GetUserPermission(ckey) == requiredRole;
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

            UserPermissionsChangedEvent permissionsChangedEvent = new(_userPermissions);
            permissionsChangedEvent.Invoke(this);
        }

        private void SyncUserPermissions(Dictionary<string, ServerRoleTypes> oldValue, Dictionary<string, ServerRoleTypes> newValue, bool asBool)
        {
            _userPermissions = newValue;

            UserPermissionsChangedEvent permissionsChangedEvent = new(_userPermissions);
            permissionsChangedEvent.Invoke(this);
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
