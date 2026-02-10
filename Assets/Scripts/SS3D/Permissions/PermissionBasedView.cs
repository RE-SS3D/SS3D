using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Permissions
{
    public class PermissionBasedView : MonoBehaviour
    {
        /// <summary>
        /// Minimum role required to see allowed objects.
        /// </summary>
        [SerializeField] private ServerRoleTypes _requiredRole;
        /// <summary>
        /// Objects shown when the user meets the required role.
        /// </summary>
        [SerializeField] private List<GameObject> _allowedObjects;
        /// <summary>
        /// Objects shown when the user does not meet the required role.
        /// </summary>
        [SerializeField] private List<GameObject> _deniedObjects;

        private IEnumerator Start()
        {
            UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);

            return InitialUpdate();
        }

        /// <summary>
        /// Performs the initial update of the permission-based view after permissions have been loaded.
        /// </summary>
        /// <returns>An IEnumerator for coroutine handling.</returns>
        private IEnumerator InitialUpdate()
        {
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            if (!permissionSystem.HasLoadedPermissions)
            {
                yield return new WaitUntil(() => permissionSystem.HasLoadedPermissions);
            }

            string ckey = Core.Settings.LocalPlayer.Ckey;
            if (string.IsNullOrEmpty(ckey))
            {
                Log.Warning(this, "Local player ckey is null or empty, cannot initialize permission-based view", Logs.UI);
                SetVisibilityForRole(ServerRoleTypes.None);
                yield break;
            }

            if (permissionSystem.TryGetUserRole(ckey, out ServerRoleTypes role))
            {
                SetVisibilityForRole(role);
            }
            else
            {
                SetVisibilityForRole(ServerRoleTypes.None);
            }
        }

        /// <summary>
        /// Handles updates when user permissions change.
        /// </summary>
        private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
        {
            string ckey = Core.Settings.LocalPlayer.Ckey;

            if (string.IsNullOrEmpty(ckey))
            {
                Log.Warning(this, "Local player ckey is null or empty, cannot update permission-based view", Logs.UI);
                return;
            }

            ServerRoleTypes role = ServerRoleTypes.None;
            e.Permissions?.TryGetValue(ckey, out role);

            Log.Information(this, "Permission view updated for {ckey} with role {role}", Logs.UI, ckey, role);
            SetVisibilityForRole(role);
        }

        /// <summary>
        /// Updates the visibility of objects based on the given role.
        /// </summary>
        /// <param name="role">The role to determine visibility.</param>
        private void SetVisibilityForRole(ServerRoleTypes role)
        {
            bool isAllowed = role >= _requiredRole;
            SetActive(_allowedObjects, isAllowed);
            SetActive(_deniedObjects, !isAllowed);
        }

        /// <summary>
        /// Sets the active state of a list of GameObjects.
        /// </summary>
        /// <param name="objects">The list of GameObjects to set active or inactive.</param>
        /// <param name="state">The active state to set.</param>
        private void SetActive(List<GameObject> objects, bool state)
        {
            if (objects == null)
            {
                return;
            }

            foreach (GameObject o in objects)
            {
                if (o == null)
                {
                    continue;
                }

                o.SetActive(state);
            }
        }
    }
}
