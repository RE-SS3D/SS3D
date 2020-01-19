using System;
using Login.Data;
using Mirror;
using UnityEngine;

namespace Login
{
    /// <summary>
    /// MonoBehaviour responsible for communication between Login UI (LoginWindow) and the backend (LoginServerClient).
    /// Begins and ends the Login process, notifies NetworkManager about selected character with a callback.
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

        void Start()
        {
            loginServerClient = new LoginServerClient(apiAddress);
        }

        /// <summary>
        /// Checks if API is reachable, updates address if true, returns result.
        /// Used as an initializer for the LoginManager both on Server and Client side.
        /// </summary>
        /// <param name="address">Address of the Login API</param>
        /// <param name="connection">The connection that will use this login process. Required for Spawn Player callback</param>
        /// <param name="spawnPlayerAction">The callback to perform once login process is complete</param>
        /// <returns></returns>
        public bool UpdateApiAddress(string address, NetworkConnection connection, Action<NetworkConnection, CharacterResponse> spawnPlayerAction)
        {
            LoginServerClient newClient = new LoginServerClient(address);

            if (newClient.IsLoginServerAlive())
            {
                apiAddress = address;
                loginServerClient = newClient;
                this.spawnPlayerAction = spawnPlayerAction;
                this.connection = connection;
                return true;
            }

            return false;
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
    }
}
