using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Messages;
using SS3D.Systems.Permissions;
using SS3D.Systems.Permissions.Events;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.UI.Buttons;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    public class JoinAsAdminView : NetworkedSpessBehaviour
    {
        [SerializeField] private LabelButton _joinAsAdminButton;

        private bool _canAccess;

        protected override void OnAwake()
        {
            base.OnAwake();

            _joinAsAdminButton.OnPressedUp += HandleJoinAsAdminButtonPressed;
            
            SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
            UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);
            ServerSoulsChanged.AddListener(HandleServerSoulsChanged);
        }

        private void HandleServerSoulsChanged(ref EventContext context, in ServerSoulsChanged e)
        {
            CheckUserPermission();
            UpdateJoinAsAdminButton();
        }

        protected override void OnStart()
        {
            base.OnStart();

            CheckUserPermission();
        }

        private void CheckUserPermission()
        {
            PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();
            bool isUserAuthorized = permissionSystem.IsUserAuthorized(LocalConnection, ServerRoleTypes.Administrator);

            _canAccess = isUserAuthorized;
        }

        private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
        {
            CheckUserPermission();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            UpdateJoinAsAdminButton();
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        { 
            UpdateJoinAsAdminButton();
        }

        private void HandleJoinAsAdminButtonPressed(bool pressed)
        {
            Soul author = GameSystems.Get<PlayerControlSystem>().GetSoul(LocalConnection);

            RequestJoinAsAdmin message = new(author);
            ClientManager.Broadcast(message);
        }

        private void UpdateJoinAsAdminButton()
        {
            if (!_canAccess)
            {
                _joinAsAdminButton.Disabled = true;
                return;
            }

            EntitySpawnSystem spawnSystem = GameSystems.Get<EntitySpawnSystem>();
            RoundSystem roundSystem = GameSystems.Get<RoundSystem>();

            bool isPlayedSpawned = spawnSystem.IsPlayerSpawned(LocalConnection);
            RoundState roundState = roundSystem.RoundState;

            if (roundState == RoundState.Stopped)
            {
                _joinAsAdminButton.Disabled = false;
            }

            if (roundState != RoundState.Ongoing)
            {
                _joinAsAdminButton.Disabled = true;
            }

            if (roundState == RoundState.Ongoing && isPlayedSpawned)
            {
                _joinAsAdminButton.Disabled = true;
            }

            if (roundState == RoundState.Ongoing && !isPlayedSpawned)
            {
                _joinAsAdminButton.Disabled = false;
            }
        }
    }
}