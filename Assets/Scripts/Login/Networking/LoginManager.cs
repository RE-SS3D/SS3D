using System;
using Login.Data;
using Mirror;
using UnityEngine;

namespace Login
{
    /// <summary>
    /// MonoBehaviour responsible for beginning and ending the Login process, notifying NetworkManager about selected character with a callback.
    /// Should be attached to Login System prefab.
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        public LoginServerClient LoginServerClient => loginServerClient;
        
        [SerializeField] private LoginWindow loginWindowPrefab;
        
        private LoginServerClient loginServerClient;
        private LoginWindow loginWindowInstance;
        private Action<NetworkConnection, CharacterResponse> spawnPlayerAction;
        private NetworkConnection connection;
        
        private string apiAddress;
        private string token;

        private void Start()
        {
            loginServerClient = new LoginServerClient(apiAddress, this);
        }

        /// <summary>
        /// Used as an initializer for the LoginManager both on Server and Client side.
        /// </summary>
        public void UpdateApiAddress(string address, NetworkConnection connection, Action<NetworkConnection, CharacterResponse> spawnPlayerAction)
        {
            loginServerClient = new LoginServerClient(address, this);
            apiAddress = address;
            this.spawnPlayerAction = spawnPlayerAction;
            this.connection = connection;
        }

        /// <summary>
        /// Checks if the API is reachable. Consumes callback to handle the response.
        /// </summary>
        public void ApiHeartbeat(Action<string, bool> callback)
        {
            if (loginServerClient == null)
            {
                Debug.LogError("Attempting to get heartbeat without setting API address!");
                return;
            }

            loginServerClient.Heartbeat(callback);
        }

        public void ShowLoginWindow()
        {
            loginWindowInstance = Instantiate(loginWindowPrefab, transform);
        }

        private void HideLoginWindow()
        {
            Destroy(loginWindowInstance.gameObject);
        }

        public void SpawnPlayer(CharacterResponse characterResponse)
        {
            spawnPlayerAction(connection, characterResponse);
            HideLoginWindow();
        }

        public void StoreToken(string token)
        {
            loginServerClient.StoreToken(token);
        }
    }
}
