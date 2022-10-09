using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Permissions.Events;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;

namespace SS3D.Systems.Permissions
{
    /// <summary>
    /// TODO: Make a simple permission system based on a .txt file to avoid non-admins from starting a round
    /// </summary>
    public sealed class PermissionSystem : NetworkedSystem
    {
        private const string EditorPermissionFilePath = "/Builds/Config/permissions.txt";
        private const string PermissionFilePath = "/Config/permissions.txt";

        private static string FullPermissionFilePath => Path.GetFullPath(".") + (Application.isEditor ? EditorPermissionFilePath : PermissionFilePath);

        [SyncObject]
        private readonly SyncDictionary<string, ServerRoleTypes> _userPermissions = new();

        protected override void OnStart()
        {
            base.OnStart();

            _userPermissions.OnChange += HandleOnChange;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            SyncUserPermissions();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            LoadPermissions();
        }

        private void HandleOnChange(SyncDictionaryOperation op, string key, ServerRoleTypes value, bool asServer)
        {
              SyncUserPermissions();
        }

        public ServerRoleTypes GetUserPermission(string ckey)
        {
            if (_userPermissions.Count == 0 || _userPermissions == null)
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

        private void SyncUserPermissions()
        {
            Dictionary<string, ServerRoleTypes> dictionary = _userPermissions.ToDictionary(pair => pair.Key, pair => pair.Value);

            UserPermissionsChangedEvent permissionsChangedEvent = new(dictionary);
            permissionsChangedEvent.Invoke(this);
        }

        private static void CreatePermissionsFileIfNotExists()
        {
            if (File.Exists(FullPermissionFilePath))
            {
                return;
            }

            Debug.Log($"[{nameof(PermissionSystem)}] - Permissions file not found, creating a new one");
            File.WriteAllText(FullPermissionFilePath, string.Empty);
        }
    }
}
