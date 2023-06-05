using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
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
            _ckey = LocalPlayer.Ckey;

            DisableObjects();
        }

        private void DisableObjects()
        {
            if (_ckey == null)
            {
                return;
            }

            PermissionSubSystem permissionSystem = Subsystems.Get<PermissionSubSystem>();

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
