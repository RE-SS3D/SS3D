using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Utils;
using SS3D.Systems.Permissions.Events;
using UnityEngine;

namespace SS3D.Systems.Permissions
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
            _ckey = LocalPlayerAccountUtility.Ckey;

            DisableObjects();
        }

        private void DisableObjects()
        {
            if (_ckey == null)
            {
                return;
            }

            PermissionSystem permissionSystem = SystemLocator.Get<PermissionSystem>();

            if (!permissionSystem.HasLoadedPermissions)
            {
                return;
            }

            permissionSystem.TryGetUserRole(_ckey, out ServerRoleTypes role);

            if (role == ServerRoleTypes.Administrator)
            {
                return;
            }

            foreach (GameObject o in _objectsToDisable.Where(o => o != null))
            {
                o.Destroy();
            }
        }
    }
}
