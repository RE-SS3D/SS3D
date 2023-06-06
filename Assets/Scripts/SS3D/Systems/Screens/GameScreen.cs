using Coimbra.Services.Events;
using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens.Events;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// Controls a screen that can be activated and deactivated globally using events
    /// </summary>
    public class GameScreen : Actor
    {
        [FormerlySerializedAs("_screenType")]
        public ScreenType ScreenType;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _holder;

        private Sequence _sequence;

        private const float ScaleInScale = 1.15f;
        private const float FadeDuration = .05f;
        private const float ScaleDuration = .175f;

        protected override void OnStart()
        {
            base.OnStart();

            GameScreens.Register(this);

            Setup();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            GameScreens.Unregister(ScreenType);
        }

        [ServerOrClient]
        private void Setup()
        {
            bool foundCanvas = TryGetComponent(out CanvasGroup canvasGroup);
            _canvasGroup = foundCanvas ? canvasGroup : GameObject.AddComponent<CanvasGroup>();
        }

        [ServerOrClient]
        public void SetScreenState(ScreenType nextScreen, bool forceInstant = false)
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            bool matchesScreenType = nextScreen == ScreenType;

            float fadeDuration = forceInstant ? 0 : FadeDuration;
            float scaleDuration = forceInstant ? 0 : ScaleDuration;

            float targetFade = matchesScreenType ? 1 : 0;
            float targetScale = matchesScreenType ? 1 : ScaleInScale;

            _holder.DOScale(targetScale, scaleDuration).SetEase(Ease.OutQuart);

            _canvasGroup.DOFade(targetFade, fadeDuration).SetEase(Ease.OutCirc);
            _canvasGroup.interactable = matchesScreenType;
            _canvasGroup.blocksRaycasts = matchesScreenType;
        }
    }
}