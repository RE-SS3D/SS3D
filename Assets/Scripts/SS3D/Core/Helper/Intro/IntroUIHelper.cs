using DG.Tweening;
using UnityEngine;

namespace SS3D.Core.Helper.Intro
{
    /// <summary>
    /// This class simply manages the UI in the intro
    /// </summary>
    public sealed class IntroUIHelper : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup _introUiFade;
        [SerializeField] private CanvasGroup _connectionUiFade;
        
        [Header("Settings")]
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        [SerializeField] private float _splashScreenFreezeDuration;

        [Header("Temporary")]
        [SerializeField] private AudioSource _temporaryAudioSource;

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            if (ApplicationStateManager.Instance.SkipIntro)
            {
                Destroy(_temporaryAudioSource);
                ApplicationStateManager.Instance.InitializeApplication();
            }
            else
            {
                TurnOnConnectionUIAfterFade();
            }
        }

        // Please don't mess with this, its disgusting
        private void TurnOnConnectionUIAfterFade()
        {
            _introUiFade.alpha = 0;

            _introUiFade.DOFade(1, _fadeInDuration).SetEase(Ease.InExpo).OnComplete(() =>
            {
                _introUiFade.DOFade(0, _fadeOutDuration).SetDelay(_splashScreenFreezeDuration).OnComplete(() =>
                {
                    _connectionUiFade.DOFade(1, _fadeInDuration);
                }).OnComplete(ApplicationStateManager.Instance.InitializeApplication);
            }).SetEase(Ease.InCubic);
        }
    }
}
