using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Screens.Events;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public sealed class GameScreensController : NetworkActor
    {
        [SerializeField] private bool _blockNone;
        [SerializeField] private bool _menuOpen;

        protected override void OnStart()
        {
            base.OnStart();

            _menuOpen = true;
            _blockNone = true;

            ChangeGameScreenEvent.AddListener(HandleChangeGameScreen);
            SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated);
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();
            Soul soul = playerControlSystem.GetSoul(LocalConnection);

            if (soul == null)
            {
                return;
            }

            bool isPlayerSpawned = e.SpawnedPlayers.Contains(soul.Ckey);

            if (!isPlayerSpawned)
            {
                _menuOpen = true;
            }

            _blockNone = !isPlayerSpawned;
            UpdateScreen();
        }

        private void HandleChangeGameScreen(ref EventContext context, in ChangeGameScreenEvent e)
        {
            ScreenType screenType = e.Screen;

            if (screenType == ScreenType.None)
            {
                _menuOpen = false;
            }

            if (screenType == ScreenType.Lobby)
            {
                _menuOpen = true;
            }
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            ProcessInput();
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !_blockNone)
            {
                _menuOpen = !_menuOpen;

                UpdateScreen();
            }
        }

        private void UpdateScreen()
        {
            ScreenType newScreen = _menuOpen ? ScreenType.Lobby : ScreenType.None;
            ChangeGameScreenEvent changeGameScreenEvent = new(newScreen);

            changeGameScreenEvent.Invoke(this);
        }
    }
}
