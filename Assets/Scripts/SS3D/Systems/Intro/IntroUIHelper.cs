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

            // Multiplayer harness drives join/reconnect via AutomationSubSystem (-testscript= or an
            // MPPM smoke: tag). Auto-starting here races that path while the script's reconnect is
            // still pending.
            if (!string.IsNullOrEmpty(applicationSettings.ActiveTestScriptPath))
            {
                return;
            }

            if (applicationSettings.SkipIntro)
            {
                Destroy(_temporaryAudioSource);

                TryStartNetworkSessionOnce();

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
                    TryStartNetworkSessionOnce();

                    _connectionUiCanvasGroup.DOFade(1, _fadeInDuration).SetDelay(2);
                });
            }).SetEase(Ease.InCubic);
        }

        private static void TryStartNetworkSessionOnce()
        {
            // Cold-start only: never auto-join on reconnect / WaitingForServer.
            ClientConnectionRecovery recovery = ClientConnectionRecovery.Instance;
            if (recovery != null && recovery.State != SessionState.Cold)
            {
                return;
            }

            // StartNetworkSession no-ops itself when the session cannot start;
            // still null-check in case Intro is open without a session subsystem.
            NetworkSessionSubSystem session = SubSystems.Get<NetworkSessionSubSystem>();
            if (session == null)
            {
                return;
            }

            session.StartNetworkSession();
        }
    }
}
