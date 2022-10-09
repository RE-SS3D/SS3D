using System.Collections.Generic;
using Coimbra;
using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Utils;
using SS3D.Logging;
using SS3D.Systems.Permissions;
using SS3D.Systems.Permissions.Events;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

public class DisableIfNotAdmin : NetworkedSpessBehaviour
{
    private string _ckey;

    [SerializeField] private List<GameObject> _objectsToDisable;

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        ClientManager.RegisterBroadcast<UserJoinedServerMessage>(HandleUserJoinedServer);
        UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);
    }

    private void HandleUserJoinedServer(UserJoinedServerMessage joinedServerMessage)
    {
        if (joinedServerMessage.Ckey != LocalPlayerAccountUtility.Ckey)
        {
            return;
        }
        
        _ckey = joinedServerMessage.Ckey;
        DisableObjects();
    }

    private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
    {
        DisableObjects();
    }

    private void DisableObjects()
    {
        if (_ckey == null)
        {
            return;
        }

        PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();
        PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();

        bool isAdmin = permissionSystem.CanUserPerformAction(ServerRoleTypes.Administrator, _ckey);

        if (isAdmin)
        {
            return;
        }

        foreach (GameObject o in _objectsToDisable)
        {
            if (o != null)
            {
                o.Destroy();
            }
        }
    }
}
