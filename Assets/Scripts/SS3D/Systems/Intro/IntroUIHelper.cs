using Coimbra;
using DG.Tweening;
using SS3D.Application;
using SS3D.Core;
using SS3D.Networking;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Intro
{
    /// <summary>
    /// This class simply manages the UI in the intro
    /// </summary>
    public sealed class IntroUIHelper : MonoBehaviour
    {
        [FormerlySerializedAs("_introUiFade")]
        [Header("UI")]
        [SerializeField] private CanvasGroup _introUiCanvasGroup;
        [FormerlySerializedAs("_connectionUiFade")] [SerializeField] private CanvasGroup _connectionUiCanvasGroup;

        [Header("Settings")]
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        [SerializeField] private float _splashScreenFreezeDuration;

        [Header("Temporary")]
        [SerializeField] private AudioSource _temporaryAudioSource;

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

             if (applicationSettings.SkipIntro)
             {
                 Destroy(_temporaryAudioSource);
            
                SubSystems.Get<NetworkSessionSubSystem>().StartNetworkSession();
            
                 _introUiCanvasGroup.alpha = 0;
                 _connectionUiCanvasGroup.alpha = 1;
             }
             else
             {
                 TurnOnConnectionUIAfterFade();
             }
        }

        // Please don't mess with this, its disgusting
        private void TurnOnConnectionUIAfterFade()
        {
            _introUiCanvasGroup.alpha = 0;

            _introUiCanvasGroup.DOFade(1, _fadeInDuration).SetEase(Ease.InExpo).OnComplete(() =>
            {
                _introUiCanvasGroup.DOFade(0, _fadeOutDuration).SetDelay(_splashScreenFreezeDuration).OnComplete(() =>
                {
                    SubSystems.Get<NetworkSessionSubSystem>().StartNetworkSession();

                    _connectionUiCanvasGroup.DOFade(1, _fadeInDuration).SetDelay(2);
                });
            }).SetEase(Ease.InCubic);
        }
    }
}
