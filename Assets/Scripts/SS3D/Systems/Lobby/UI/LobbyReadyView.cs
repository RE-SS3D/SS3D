using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI.Buttons;
using UnityEngine;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Lobby.UI
{
    public sealed class LobbyReadyView : NetworkActor
    {
        [SerializeField] private ToggleLabelButton _readyButton;
        [SerializeField] private LabelButton _embarkButton;

        protected override void OnAwake()           
        {
            base.OnAwake();

            _readyButton.OnPressedDown += HandleReadyButtonPressed;
            _embarkButton.OnPressedDown += HandleEmbarkButtonPressed;

            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated));
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            _readyButton.OnPressedDown -= HandleReadyButtonPressed;
            _embarkButton.OnPressedDown -= HandleEmbarkButtonPressed;
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            ProcessSpawnedPlayers();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            ProcessRoundState(e.RoundState);
        }

        private void ProcessSpawnedPlayers()
        {
            EntitySpawnSystem spawnSystem = SystemLocator.Get<EntitySpawnSystem>();

            bool isPlayedSpawned = spawnSystem.IsPlayedSpawned(LocalConnection);

            if (isPlayedSpawned)
            {
                _readyButton.Disabled = true;
                _readyButton.SetActive(true);
                _embarkButton.Disabled = true;
                _embarkButton.SetActive(false);
            }
        }

        private void ProcessRoundState(RoundState roundState)
        {
            EntitySpawnSystem spawnSystem = SystemLocator.Get<EntitySpawnSystem>();

            bool isPlayedSpawned = spawnSystem.IsPlayedSpawned(LocalConnection);

            if (isPlayedSpawned && roundState == RoundState.Ongoing)
            {
                _readyButton.Disabled = true;
                _readyButton.SetActive(true);
                _embarkButton.Disabled = true;
                _embarkButton.SetActive(false);
            }

            if (!isPlayedSpawned && roundState == RoundState.Ongoing)
            {
                _readyButton.Disabled = true;
                _readyButton.SetActive(false);
                _embarkButton.Disabled = false;
                _embarkButton.SetActive(true);
            }

            if (roundState == RoundState.Stopped)
            {
                _readyButton.Pressed = false;
                _readyButton.Disabled = false;
                _readyButton.SetActive(true);

                _embarkButton.Pressed = false;
                _embarkButton.Disabled = true;
                _embarkButton.Highlighted = false;

                _embarkButton.SetActive(false);
            }
        }

        private void HandleEmbarkButtonPressed(bool pressed)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();

            string ckey = playerControlSystem.GetCkey(LocalConnection);
            RequestEmbarkMessage requestEmbarkMessage = new(ckey);

            ClientManager.Broadcast(requestEmbarkMessage);
        }

        private void HandleReadyButtonPressed(bool pressed)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();

            string ckey = playerControlSystem.GetCkey(LocalConnection);
            ChangePlayerReadyMessage playerReadyMessage = new(ckey, pressed);

            ClientManager.Broadcast(playerReadyMessage);
        }
    }
}
