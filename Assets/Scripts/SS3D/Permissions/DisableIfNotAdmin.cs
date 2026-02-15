using Coimbra;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserPermissionsChangedEvent = SS3D.Permissions.Events.UserPermissionsChangedEvent;

namespace SS3D.Permissions
{
    public class DisableIfNotAdmin : NetworkActor
    {
        private string _ckey;

        [SerializeField] private List<GameObject> _objectsToDisable;

        protected override void OnAwake()
        {
            base.OnAwake();

            UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);
        }

        private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
        {
            _ckey = LocalPlayer.Ckey;

            DisableObjects();
        }

        private void DisableObjects()
        {
            if (_ckey == null)
            {
                return;
            }

            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            if (!permissionSystem.HasLoadedPermissions)
            {
                return;
            }

            if (permissionSystem.IsAtLeast(_ckey, ServerRoleTypes.Administrator))
            {
                return;
            }

            foreach (GameObject o in _objectsToDisable.Where(o => o != null))
            {
                o.Dispose(true);
            }
        }
    }
}
