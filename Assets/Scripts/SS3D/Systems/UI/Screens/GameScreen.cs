using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.UI.Screens
{
    /// <summary>
    /// Controls a screen that can be activated and deactivated globally using events
    /// </summary>
    public class GameScreen : SpessBehaviour
    {
        [SerializeField] private ScreenType _screenType;
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float FadeDuration = .65f;

        protected override void OnStart()
        {
            base.OnStart();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            new ChangeGameScreenEvent(ScreenType.Lobby).Invoke(this);
            ChangeGameScreenEvent.AddListener(HandleChangeGameScreen);
        }

        private void HandleChangeGameScreen(ref EventContext context, in ChangeGameScreenEvent e)
        {
            bool matchesScreenType = e.Screen == _screenType;

            SetScreenState(matchesScreenType);
        }

        private void SetScreenState(bool matchesScreenType)
        {
            float targetFade = matchesScreenType ? 1 : 0;

            _canvasGroup.DOFade(targetFade, FadeDuration);
        }
    }
}