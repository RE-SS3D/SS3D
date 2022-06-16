using Coimbra;
using DG.Tweening;
using FishNet;
using FishNet.Transporting;
using SS3D.Data.Messages;
using SS3D.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Core.Networking.UI_Helper
{
    /// <summary>
    /// Displays the process of connecting to a server on ui elements
    /// </summary>
    public sealed class ServerConnectionView : MonoBehaviour
    {
        [Header("Loading Icon")]
        [SerializeField] private Transform _loadingIcon;
        [SerializeField] private Vector3 _loadingMovement = new Vector3(0, 0, -360);
        [SerializeField] private float _loadingIconAnimationDuration;

        [Header("Buttons")] 
        [SerializeField] private UiFade _buttonsUiFade;
        [SerializeField] private GameObject _buttons;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _retryButton;
        
        [SerializeField] private TMP_Text _messageText;


        private bool _connectionFailed;
        
        private void Start()
        {
            ProcessConnectingToServer();
            Setup();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Setup()
        {
            UpdateMessageText(ApplicationMessages.Network.ConnectingToServer);
            _buttons.SetActive(false);                            
        }

        private void SubscribeToEvents()
        {
            _quitButton.onClick.AddListener(Application.Quit);
            _retryButton.onClick.AddListener(OnRetryButtonPressed);

            InstanceFinder.ClientManager.OnClientConnectionState += OnServerConnectionFailed;
        }

        private void UnsubscribeFromEvents()
        {
        }
        
        private void UpdateMessageText(string message)
        {
            _messageText.text = message;
        }

        // Loops the rotating animation using DOTween until we are ot connecting anymore
        private void ProcessConnectingToServer()
        {
            if (_connectionFailed)
            {
                return;
            }
            
            // loops a rotating animation
            _loadingIcon.DOLocalRotate(_loadingMovement, _loadingIconAnimationDuration, RotateMode.LocalAxisAdd).OnComplete(ProcessConnectingToServer).SetEase(Ease.Linear);
        }

        private void OnRetryButtonPressed()
        {
            _connectionFailed = false;
            _buttons.SetActive(false);
            _loadingIcon.gameObject.SetActive(true);
            UpdateMessageText(ApplicationMessages.Network.ConnectingToServer);
            ProcessConnectingToServer();
            
            // Uses the event service to listen to lobby events
            IEventService eventService = ServiceLocator.Shared.Get<IEventService>(); 
            eventService?.Invoke(this, new RetryServerConnectionEvent());
        }
        
        private void OnServerConnectionFailed(ClientConnectionStateArgs clientConnectionStateArgs)
        {
            if (clientConnectionStateArgs.ConnectionState != LocalConnectionState.Stopped)
            {
                return;
            }
            
            _connectionFailed = true;
            _buttons.SetActive(true);
            _loadingIcon.gameObject.SetActive(false);
            _buttonsUiFade.ProcessFade();
            UpdateMessageText(ApplicationMessages.Network.ConnectionFailed);
        }
    }
}
