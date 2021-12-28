﻿using Mirror;
using SS3D.Engine.Server.Mirror;
using TMPro;
using UnityEngine;

// PlayerCountDisplay ensures that the player count on the ServerLobby screen
// correctly displays the current and maximum amount of players.
namespace SS3D.Engine.Server.Helpers
{
    public class PlayerCountDisplay : NetworkBehaviour
    {

        // Allows for updating the server numbers when players connect / disconnect.
        [SyncVar(hook = nameof(SyncPlayerCount))] private string PlayerCount;

        // Reference to the LoginNetworkManager
        private LoginNetworkManager loginNetworkManager;

        // Text box to display the current count
        private TMP_Text Text;

        // Allows messages to appear in the debug log.
        [SerializeField] private bool showDebug = false;

        void Start()
        {
            EnsureInit();

            // Subscribe to the ClientNumbersUpdated action. Only the server has to do this.
            // Clients will be told to update by the SyncVar hook method.
            if (isServer)
            {
                if (showDebug) Debug.Log("PlayerCountDisplay > Subscribing to ClientNumbersUpdated");
                loginNetworkManager = LoginNetworkManager.LoginSingleton;
                SyncPlayerCount();
                loginNetworkManager.ClientNumbersUpdated += SyncPlayerCount;
            }
        }

        void OnDestroy()
        {

            // Unsubscribe from the ClientNumbersUpdated action.
            if (isServer)
            {
                if (showDebug) Debug.Log("PlayerCountDisplay > Unsubscribing from ClientNumbersUpdated");
                loginNetworkManager.ClientNumbersUpdated -= SyncPlayerCount;
            }
        }

        // Server-side function responsible for calculating what the new display text should be.
        [Server]
        private void SyncPlayerCount()
        {
            string newPlayerCount = NetworkServer.connections.Count + "/" + loginNetworkManager.maxConnections;
            if (showDebug) Debug.Log("PlayerCountDisplay > SyncPlayerCount server call: newPlayerCount = " + newPlayerCount);
            SyncPlayerCount(PlayerCount, newPlayerCount);
        }

        // SyncVar hook method responsible for updating the display text on screen.
        private void SyncPlayerCount(string oldPlayerCount, string newPlayerCount)
        {
            EnsureInit();
            this.PlayerCount = newPlayerCount;

            if (showDebug) Debug.Log("PlayerCountDisplay > SyncPlayerCount hook method: newPlayerCount = " + newPlayerCount);

            Text.text = newPlayerCount;
        }

        // Ensures that all initialisation has occurred before we start synchronising.
        private void EnsureInit()
        {
            // Cache the textbox
            Text = this.gameObject.GetComponent<TMP_Text>();
        }
    }
}
