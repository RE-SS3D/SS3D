using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens.Events;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public sealed class GameScreensController : SpessBehaviour
    {
        [SerializeField] private bool _menuOpen;

        protected override void OnStart()
        {
            base.OnStart();

            _menuOpen = true;
            ChangeGameScreenEvent.AddListener(HandleChangeGameScreen);
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _menuOpen = !_menuOpen;

                ScreenType newScreen = _menuOpen ? ScreenType.Lobby : ScreenType.None;
                ChangeGameScreenEvent changeGameScreenEvent = new(newScreen);

                changeGameScreenEvent.Invoke(this);
            }
        }
    }
}
