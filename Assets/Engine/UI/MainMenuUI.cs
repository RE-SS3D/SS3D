using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using Telepathy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.UI
{
    /// <summary>
    /// UI controller for join/host a SS3D session
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public TMP_InputField ipAddressInputField;

	// are we connecting to a server
        private bool connecting;
        private Animator animator;

	// UI references
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_Text joinButtonText;
        [SerializeField] private TMP_Text errorMessageText;
        
        private int toggleAnimatorID;

        private void Start()
        {
            Client.connectionFailed += OnClientFailConnection;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            toggleAnimatorID = Animator.StringToHash("Toggle");
           
        }

        public void OnJoinButtonPressed()
        {
            var uriAdress = TryParseIpAddress();
            NetworkManager.singleton.StartClient(uriAdress);

            joinButtonText.alignment = TextAlignmentOptions.Left;
            connecting = true;
            StartCoroutine(ChangeJoinText());
            
            if (animator.GetBool("ToggleError"))
                animator.SetBool("ToggleError", false);

        }

	// When we click join, this is called
        public IEnumerator ChangeJoinText()
        {
            joinButton.interactable = false;
	    // while we connect we have to display something
	    // for the user to know he is in fact connecting
            while (connecting)
            {
                joinButtonText.text = "joining.";
                yield return new WaitForSeconds(.2f);
                joinButtonText.text = "joining..";
                yield return new WaitForSeconds(.2f);
                joinButtonText.text = "joining...";
                yield return new WaitForSeconds(.2f);
            }
            joinButton.interactable = true;
            joinButtonText.alignment = TextAlignmentOptions.Midline;
            joinButtonText.text = "join";
        }

        public void OnHostButtonPressed()
        {
            NetworkManager.singleton.StartHost();

            animator?.SetTrigger(toggleAnimatorID);
        }
        
        public void OnClientFailConnection()
        {
            UnityMainThread.wkr.AddJob(delegate
            {
                connecting = false;
                animator.SetBool("ToggleError", true);

                errorMessageText.text = "Connection to the server failed";
            });
            
        }
	
	// This handles getting the IP address from the input field
	// it needs to be transformed in a Uri in order for us to send it to the NetworkManager
        private Uri TryParseIpAddress()
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "tcp4";
            if (ipAddressInputField &&
                IPAddress.TryParse(ipAddressInputField.text, out IPAddress address))
            {
                uriBuilder.Host = address.ToString();
            }
            else
            {
                uriBuilder.Host = "localhost";
            }

            var uri = new Uri(uriBuilder.ToString(), UriKind.Absolute);
            return uri;
        }
    }
}
