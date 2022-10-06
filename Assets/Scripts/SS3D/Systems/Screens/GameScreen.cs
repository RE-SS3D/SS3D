using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// Controls a screen that can be activated and deactivated globally using events
    /// </summary>
    public class GameScreen : SpessBehaviour
    {
        [SerializeField] private ScreenType _screenType;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _holder;

        public static ScreenType LastScreen { get; private set; }

        private const float ScaleInScale = 1.15f;

        private const float FadeDuration = .05f;
        private const float ScaleDuration = .175f;

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
        }

        private void HandleChangeGameScreen(ref EventContext context, in ChangeGameScreenEvent e)
        {
            ScreenType screenType = e.Screen;

            SetScreenState(screenType);
        }

        private void Setup()
        {
            LastScreen = ScreenType.None;

            if (_canvasGroup != null)
            {
                bool foundCanvas = TryGetComponent(out CanvasGroup canvasGroup);
                _canvasGroup = foundCanvas ? canvasGroup : GameObjectCache.AddComponent<CanvasGroup>();
            }

            SetScreenState(ScreenType.Lobby, true);
            ChangeGameScreenEvent.AddListener(HandleChangeGameScreen);
        }

        private void SetScreenState(ScreenType nextScreen, bool forceInstant = false)
        {
            LastScreen = nextScreen;

            bool matchesScreenType = nextScreen == _screenType;
            float fadeDuration = forceInstant ? 0 : FadeDuration;
            float scaleDuration = forceInstant ? 0 : ScaleDuration;

            float targetFade = matchesScreenType ? 1 : 0;

            float initScale = matchesScreenType ? ScaleInScale : 1;
            float targetScale = matchesScreenType ? 1 : ScaleInScale;

            _holder.localScale = new Vector3(initScale, initScale, initScale);
            _holder.DOScale(targetScale, scaleDuration).SetEase(Ease.OutQuart);

            _canvasGroup.DOFade(targetFade, fadeDuration).SetEase(Ease.OutCirc);
            _canvasGroup.interactable = matchesScreenType;
            _canvasGroup.blocksRaycasts = matchesScreenType;

            if (LastScreen == nextScreen)
            {
                Debug.Log($"[{nameof(GameScreen)}] - Game screen changed to {nextScreen}");
            }
        }
    }
}