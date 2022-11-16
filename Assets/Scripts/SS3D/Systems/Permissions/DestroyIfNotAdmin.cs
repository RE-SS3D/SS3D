using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services;
using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Utils;
using SS3D.Systems.Permissions.Events;
using UnityEngine;

namespace SS3D.Systems.Permissions
{
    public class DestroyIfNotAdmin : NetworkedSpessBehaviour
    {
        private string _ckey;

        [SerializeField] private List<GameObject> _objectsToDisable;

        private EventHandle _permissionsChangedEventHandle;

        protected override void OnAwake()
        {
            base.OnAwake();

            _permissionsChangedEventHandle = UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            IEventService eventService = ServiceLocator.Get<IEventService>();
            eventService?.RemoveListener(_permissionsChangedEventHandle);
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

            PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();

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
