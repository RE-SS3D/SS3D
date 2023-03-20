using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Screens.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Screens
{
    public sealed class GameScreensController : NetworkActor
    {
        [SerializeField] private bool _blockNone;
        [SerializeField] private bool _menuOpen;

        private PlayerSpawnedState _spawnedState;
        private Controls.OtherActions _controls;

        protected override void OnAwake()
        {
            base.OnAwake();

            _menuOpen = true;
            _blockNone = true;
            _spawnedState = PlayerSpawnedState.IsNotSpawned;

            ChangeGameScreenEvent.AddListener(HandleChangeGameScreen);
            SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);

            _controls = Subsystems.Get<InputSystem>().Inputs.Other;
            _controls.ToggleMenu.performed += HandleToggleMenu;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            _controls.ToggleMenu.performed -= HandleToggleMenu;
        }

        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (!_blockNone)
            {
                _menuOpen = !_menuOpen;
                UpdateScreen();
            }
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
                    UpdateScreen();
                    break;
            }
        }

        private void HandleChangeGameScreen(ref EventContext context, in ChangeGameScreenEvent e)
        {
            ScreenType screenType = e.Screen;

            if (screenType == ScreenType.None)
            {
                _menuOpen = false;
                MarkNewlySpawnedPlayerAsAwaitingConfirmation();
            }

            if (screenType == ScreenType.Lobby)
            {
                _menuOpen = true;
            }
        }


        private void UpdateScreen()
        {
            ScreenType newScreen = _menuOpen ? ScreenType.Lobby : ScreenType.None;
            ChangeGameScreenEvent changeGameScreenEvent = new(newScreen);

            changeGameScreenEvent.Invoke(this);
        }

        /// <summary>
        /// Prevents the player from leaving the menu screen.
        /// </summary>
        private void LockToMenuScreen()
        {
            _blockNone = true;
            _menuOpen = true;
            _spawnedState = PlayerSpawnedState.IsNotSpawned;
        }

        /// <summary>
        /// Gives the player the ability to toggle in and out of the
        /// menu, and records that they have been added to the Spawned
        /// Players list.
        /// </summary>
        private void GivePlayerAccessToGame()
        {
            _blockNone = false;
            _spawnedState = PlayerSpawnedState.ConfirmedSpawned;
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
