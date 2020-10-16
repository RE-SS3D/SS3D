using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

namespace SS3D.UI
{
    /// <summary>
    /// UI controller for join/host SS3D session
    /// </summary>
    public class LobbyLoginUI : MonoBehaviour
    {
        public TMP_InputField ipAddressInputField;

        private Animator animator;
        private int toggleAnimatorID;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            toggleAnimatorID = Animator.StringToHash("Toggle");
        }

        public void OnJoinButtonPressed()
        {
            var uriAdress = TryParseIpAddress();
            NetworkManager.singleton.StartClient(uriAdress);

            animator?.SetTrigger(toggleAnimatorID);
        }

        public void OnHostButtonPressed()
        {
            NetworkManager.singleton.StartHost();

            animator?.SetTrigger(toggleAnimatorID);
        }

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
