using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Screens.Events;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Screens
{
    public sealed class GameScreensController : NetworkActor
    {
        [SerializeField] private bool _blockSwitchToNone;

        private PlayerSpawnedState _spawnedState;
        private Controls.OtherActions _controls;

        protected override void OnAwake()
        {
            base.OnAwake();

            _blockSwitchToNone = true;
            _spawnedState = PlayerSpawnedState.IsNotSpawned;

            AddHandle(ChangeGameScreenEvent.AddListener(HandleChangeGameScreen));
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));

            _controls = SubSystems.Get<InputSubSystem>().Inputs.Other;
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            
            _controls.ToggleMenu.performed += HandleToggleMenu;
        }
        
        protected override void OnDisabled()
        {
            base.OnDisabled();
            
            _controls.ToggleMenu.performed -= HandleToggleMenu;
        }

        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (_blockSwitchToNone)
            {
                return;
            }

            ScreenType screenToSwitchTo = GameScreens.ActiveScreen == ScreenType.Lobby ? ScreenType.None : ScreenType.Lobby;
            GameScreens.SwitchTo(screenToSwitchTo);
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            bool isPlayerSpawned = e.SpawnedPlayers.Find(controllable => controllable.Owner == LocalConnection);

            if (!isPlayerSpawned && _spawnedState == PlayerSpawnedState.ConfirmedSpawned)
            {
                LockToMenuScreen();
            }

            if (isPlayerSpawned)
            {
                GivePlayerAccessToGame();
            }

            UpdateScreen();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case Rounds.RoundState.Ongoing:
                case Rounds.RoundState.Ending:
                    break;
                default:
                    LockToMenuScreen();
                    break;
            }
        }

        private void HandleChangeGameScreen(ref EventContext context, in ChangeGameScreenEvent e)
        {
            ScreenType screenType = e.Screen;

            if (screenType == ScreenType.None)
            {
                MarkNewlySpawnedPlayerAsAwaitingConfirmation();
            }
        }


        private void UpdateScreen()
        {
            switch (_spawnedState)
            {
                case PlayerSpawnedState.IsNotSpawned:
                case PlayerSpawnedState.AwaitingConfirmationOfSpawn:
                    GameScreens.SwitchTo(ScreenType.Lobby);
                    break;
                case PlayerSpawnedState.ConfirmedSpawned:
                    GameScreens.SwitchTo(ScreenType.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Prevents the player from leaving the menu screen.
        /// </summary>
        private void LockToMenuScreen()
        {
            _blockSwitchToNone = true;
            _spawnedState = PlayerSpawnedState.IsNotSpawned;

            UpdateScreen();
        }

        /// <summary>
        /// Gives the player the ability to toggle in and out of the
        /// menu, and records that they have been added to the Spawned
        /// Players list.
        /// </summary>
        private void GivePlayerAccessToGame()
        {
            _blockSwitchToNone = false;
            _spawnedState = PlayerSpawnedState.ConfirmedSpawned;

            UpdateScreen();
        }

        /// <summary>
        /// Identifies that the entity may have spawned recently, and
        /// may not yet been reflected in the Spawned Players list.
        /// </summary>
        private void MarkNewlySpawnedPlayerAsAwaitingConfirmation()
        {
            if (_spawnedState == PlayerSpawnedState.IsNotSpawned)
            {
                _spawnedState = PlayerSpawnedState.AwaitingConfirmationOfSpawn;
            }

            UpdateScreen();
        }

        /// <summary>
        /// Internal enum to describe player spawn state.
        /// </summary>
        private enum PlayerSpawnedState
        {
            IsNotSpawned,
            AwaitingConfirmationOfSpawn,
            ConfirmedSpawned
        }
    }
}
