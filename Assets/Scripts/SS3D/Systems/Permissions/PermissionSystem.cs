using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Logging;
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
        [SyncVar] public bool HasLoadedPermissions;

        protected override void OnStart()
        {
            base.OnStart();

            _userPermissions.OnChange += HandleUserPermissionsChanged;
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

        private void HandleUserPermissionsChanged(SyncDictionaryOperation op, string key, ServerRoleTypes value, bool asServer)
        {
            SyncUserPermissions();
        }

        public bool TryGetUserRole(string ckey, out ServerRoleTypes userPermission)
        {
            if (_userPermissions.Count == 0 || _userPermissions == null)
            {
                LoadPermissions();
            }

            bool containsKey = _userPermissions.ContainsKey(ckey);
            userPermission = containsKey ? _userPermissions[ckey] : ServerRoleTypes.None;

            return containsKey;
        }

        [Server]
        public void ChangeUserPermission(string ckey, ServerRoleTypes role)
        {
            // TODO: This            
            // Add new user permission to list
            // Add new user permission to text file
        }

        [Server]
        private void LoadPermissions()
        {
            CreatePermissionsFileIfNotExists();

            string[] lines = File.ReadAllLines(FullPermissionFilePath);

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                string[] words = line.Split(" ");

                string ckey = words[0];
                Enum.TryParse(words[1], out ServerRoleTypes role);

                _userPermissions.Add(ckey, role);

                Punpun.Say(this, $"Found user permission {ckey} as {role}", Logs.ServerOnly);
            }

            HasLoadedPermissions = true;
            SyncUserPermissions();
        }

        private void SyncUserPermissions()
        {
            Dictionary<string, ServerRoleTypes> dictionary = _userPermissions.ToDictionary(pair => pair.Key, pair => pair.Value);

            UserPermissionsChangedEvent permissionsChangedEvent = new(dictionary);
            permissionsChangedEvent.Invoke(this);
        }

        [Server]
        private void CreatePermissionsFileIfNotExists()
        {
            if (File.Exists(FullPermissionFilePath))
            {
                return;
            }

            Punpun.Say(this, $"Permissions file not found, creating a new one", Logs.ServerOnly);
            File.WriteAllText(FullPermissionFilePath, string.Empty);
        }
    }
}
