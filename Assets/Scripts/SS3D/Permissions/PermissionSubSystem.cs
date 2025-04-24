using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Logging;
using SS3D.Permissions.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using File = System.IO.File;
using UserPermissionsChangedEvent = SS3D.Permissions.Events.UserPermissionsChangedEvent;

namespace SS3D.Permissions
{
    /// <summary>
    /// Handles user permission on what he can do and can't.
    /// </summary>
    public sealed class PermissionSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// Dictionary of users and permissions.
        /// </summary>
        [SyncObject]
        private readonly SyncDictionary<string, ServerRoleTypes> _userPermissions = new();

        /// <summary>
        /// If the server has loaded the permissions list
        /// </summary>
        [SyncVar]
        public bool HasLoadedPermissions;

        /// <summary>
        /// File name to the permissions file.
        /// TODO: Move this to a PermissionSettings and create permissions via JSON.
        /// </summary>
        private const string ConfigFileName = "permissions.txt";

        private static readonly string PermissionsPath = Paths.GetPath(GamePaths.Config, true) + "/" + ConfigFileName;

        protected override void OnStart()
        {
            base.OnStart();

            _userPermissions.OnChange += SyncHandleUserPermissionsChanged;
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

        /// <summary>
        /// Tries to get a user role.
        /// </summary>
        /// <param name="ckey">The user to get the role of.</param>
        /// <param name="userPermission">The access level of that user.</param>
        /// <returns>If the user role was found or not.</returns>
        public bool TryGetUserRole(string ckey, out ServerRoleTypes userPermission)
        {
            if (_userPermissions.Count == 0 || _userPermissions == null)
            {
                LoadPermissions();
            }

            if (string.IsNullOrEmpty(ckey))
            {
                Log.Warning(this, "Ckey null while trying to get user role");

                userPermission = ServerRoleTypes.None;
                return false;
            }

            bool containsKey = _userPermissions.ContainsKey(ckey);
            userPermission = containsKey ? _userPermissions[ckey] : ServerRoleTypes.None;

            return containsKey;
        }

        /// <summary>
        /// Updates a user permission.
        /// </summary>
        /// <param name="ckey">The desired user to update the permission.</param>|
        /// <param name="role">The new user role.</param>
        [Server]
        public void ChangeUserPermission(string ckey, ServerRoleTypes role)
        {
            ServerRoleTypes previousRole = _userPermissions.TryGetValue(ckey, out ServerRoleTypes permission) ? permission : ServerRoleTypes.None;

            Log.Information(this, $"Updating user {ckey} role from {previousRole} to {role}");

            _userPermissions[ckey] = role;

            SaveUserPermissions();
        }

        /// <summary>
        /// Saves the permissions text file with the updated permissions list.
        /// </summary>
        public void SaveUserPermissions()
        {
            string fileContent = string.Empty;

            foreach (KeyValuePair<string,ServerRoleTypes> userPermission in _userPermissions)
            {
                fileContent += $"{userPermission.Key} {userPermission.Value.ToString()}\n";
            }

            File.WriteAllText(PermissionsPath, fileContent);
        }

        /// <summary>
        /// Loads the user permissions found in the txt file.
        /// </summary>
        [Server]
        private void LoadPermissions()
        {
            Log.Information(this, "Loading permission data from file", Logs.ServerOnly);
            
            if (!HasLoadedPermissions)
            {
                CreatePermissionsFileIfNotExists();
            }

            string[] lines = File.ReadAllLines(PermissionsPath);

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] words = line.Split(" ");

                string ckey = words[0];
                Enum.TryParse(words[1], out ServerRoleTypes role);

                _userPermissions.Add(ckey, role);

                Log.Information(this, "Found user permission {ckey} as {role}", Logs.ServerOnly, ckey, role);
            }

            HasLoadedPermissions = true;
        }

        [Server]
        private void CreatePermissionsFileIfNotExists()
        {
            if (File.Exists(PermissionsPath))
            {
                return;
            }

            Log.Information(this, "Permissions file not found, creating a new one", Logs.ServerOnly);
            File.WriteAllText(PermissionsPath, string.Empty);
        }

        /// <summary>
        /// Used to update clients about which user has which permission.
        /// </summary>
        private void SyncUserPermissions()
        {
            Dictionary<string, ServerRoleTypes> dictionary = _userPermissions.ToDictionary(pair => pair.Key, pair => pair.Value);

            UserPermissionsChangedEvent permissionsChangedEvent = new(dictionary);
            permissionsChangedEvent.Invoke(this);
        }

        private void SyncHandleUserPermissionsChanged(SyncDictionaryOperation op, string key, ServerRoleTypes value, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            SyncUserPermissions();
        }

        /// <summary>
        /// Returns if the user is at least at a level of access.
        /// </summary>
        /// <param name="ckey">The user to check permission for</param>
        /// <param name="permissionLevelCheck">The lowest required permission to perform the action.</param>
        /// <returns></returns>
        public bool IsAtLeast(string ckey, ServerRoleTypes permissionLevelCheck)
        {
            TryGetUserRole(ckey, out ServerRoleTypes userPermission);

            return userPermission >= permissionLevelCheck;

        }
    }
}
