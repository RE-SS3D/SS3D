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
        [SerializeField] private string apiAddress;
        [SerializeField] private LoginWindow loginWindowPrefab;
        
        private LoginServerClient loginServerClient;
        private LoginWindow loginWindowInstance;
        private Action<NetworkConnection, CharacterResponse> spawnPlayerAction;
        private NetworkConnection connection;

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

        public void HideLoginWindow()
        {
            Destroy(loginWindowInstance.gameObject);
        }

        public ApiResponse CallRegisterEndpoint(RegisterCredentials registerCredentials)
        {
            return loginServerClient.Register(registerCredentials);
        }

        public ApiResponse CallAuthenticateEndpoint(LoginCredentials loginCredentials)
        {
            ApiResponse response = loginServerClient.Authenticate(loginCredentials);
            if (response.IsSuccess())
            {
                token = response.GetToken();
            }

            return response;
        }

        public ApiResponse CallCharacterListEndpoint()
        {
            if (!IsTokenValid("Get Characters"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }

            return loginServerClient.GetCharacters(token);
        }

        public ApiResponse CallCharacterCreateEndpoint(string name)
        {
            if (!IsTokenValid("Create Character"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }

            return loginServerClient.SaveCharacter(token, name);
        }

        public ApiResponse CallCharacterDeleteEndpoint(int id)
        {
            if (!IsTokenValid("Delete Character"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }

            return loginServerClient.DeleteCharacter(token, id);
        }

        public void SpawnPlayer(CharacterResponse characterResponse)
        {
            spawnPlayerAction(connection, characterResponse);
            HideLoginWindow();
        }

        /// <summary>
        /// Validates that client has authenticated and has a token. Should be called before every non-anonymous endpoint call.
        /// </summary>
        /// <param name="requestName">For logging purposes only. Specifies which request was done when error occured.</param>
        /// <returns></returns>
        private bool IsTokenValid(string requestName)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError($"Attempting to make {requestName} requests before having received a token!");
                return false;
            }

            return true;
        }

    }
}
