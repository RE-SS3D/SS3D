using System;
using UnityEngine;
using SS3D.Engine.Server.Login.Screens;
using SS3D.Engine.Server.Login.Data;

namespace SS3D.Engine.Server.Login.Networking
{
    /// <summary>
    /// MonoBehaviour responsible for beginning and ending the Login process, notifying NetworkManager about selected character with a callback.
    /// Should be attached to Login System prefab.
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        public LoginServerClient LoginServerClient { get; private set; }

        [SerializeField] private LoginWindow loginWindowPrefab = null;
        private LoginWindow loginWindowInstance;
        private Action<CharacterResponse> spawnPlayerAction;
        
        private string apiAddress;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoginServerClient = new LoginServerClient(apiAddress, this);
        }

        /// <summary>
        /// Used as an initializer for the LoginManager both on Server and Client side.
        /// </summary>
        public void UpdateApiAddress(string address, Action<CharacterResponse> spawnPlayerAction)
        {
            LoginServerClient = new LoginServerClient(address, this);
            apiAddress = address;
            this.spawnPlayerAction = spawnPlayerAction;
        }

        /// <summary>
        /// Checks if the API is reachable. Consumes callback to handle the response.
        /// </summary>
        public void ApiHeartbeat(Action<string, bool> callback)
        {
            if (LoginServerClient == null)
            {
                Debug.LogError("Attempting to get heartbeat without setting API address!");
                return;
            }

            LoginServerClient.Heartbeat(callback);
        }

        public void ShowLoginWindow()
        {
            //Get rid of previous login window, if exists
            if (loginWindowInstance) HideLoginWindow();
            
            loginWindowInstance = Instantiate(loginWindowPrefab, transform);
        }

        private void HideLoginWindow()
        {
            Destroy(loginWindowInstance.gameObject);
            loginWindowInstance = null;
        }

        public void SpawnPlayer(CharacterResponse characterResponse)
        {
            spawnPlayerAction(characterResponse);
            HideLoginWindow();
        }

        public void StoreToken(string token)
        {
            LoginServerClient.StoreToken(token);
        }
    }
}
