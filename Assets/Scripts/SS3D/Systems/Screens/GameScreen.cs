using Coimbra;
using Coimbra.Services.Events;
using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Screens.Events;
using UnityEngine;
using UnityEngine.Serialization;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// Controls a screen that can be activated and deactivated globally using events
    /// </summary>
    public class GameScreen : Actor
    {
        [SerializeField]
        private ScreenType _screenType;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Transform _holder;

        private Sequence _sequence;

        public ScreenType ScreenType => _screenType;

        [ServerOrClient]
        public void SetScreenState(ScreenType nextScreen, bool forceInstant = false)
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            bool matchesScreenType = nextScreen == _screenType;

            GameScreenSettings gameScreenSettings = ScriptableSettings.GetOrFind<GameScreenSettings>();

            float fadeDuration = forceInstant ? 0 : gameScreenSettings.FadeInOutDuration;
            float scaleDuration = forceInstant ? 0 : gameScreenSettings.ScaleInOutDuration;

            float targetFade = matchesScreenType ? 1 : 0;
            float targetScale = matchesScreenType ? 1 : gameScreenSettings.ScaleInOutScale;

            _holder.DOScale(targetScale, scaleDuration).SetEase(Ease.OutQuart);

            _canvasGroup.DOFade(targetFade, fadeDuration).SetEase(Ease.OutCirc);
            _canvasGroup.interactable = matchesScreenType;
            _canvasGroup.blocksRaycasts = matchesScreenType;
        }

        protected override void OnStart()
        {
            base.OnStart();

            GameScreens.Register(this);

            Setup();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            GameScreens.Unregister(_screenType);
        }

        [ServerOrClient]
        private void Setup()
        {
            bool foundCanvas = TryGetComponent(out CanvasGroup canvasGroup);
            _canvasGroup = foundCanvas ? canvasGroup : GameObject.AddComponent<CanvasGroup>();
        }
    }
}