using Login;
using Login.Data;
using System;
using UnityEngine;

namespace Mirror
{
    /// <summary>
    /// Custom implementation of Network manager to accomodate requiring the player to login before spawning them.
    /// Should be attached to the LoginNetworkManager prefab.
    ///
    /// The overriden events are expected to fire in this order:
    /// OnServerConnect (On the server only)
    /// OnClientConnect (On the client only)
    /// OnLoginDataMessage (On the client only)
    /// OnServerAddPlayer (On the server only)
    ///
    /// General workflow:
    /// Client connects to server
    /// Server notifies client of the login API address (if any)
    /// Client either:
    ///     a. if API does not exist or cannot be reached:
    ///         Client notifies server that they want to spawn using default method
    ///     b. if API is found and is reachable:
    ///         Client begins Login procedure, handled by the LoginManager
    ///         Client notifies server that they want to spawn and sends extra character selection data
    ///Server spawns client
    ///END         
    ///
    /// All other functionality is standard Mirror NetworkManager behaviour
    /// </summary>
    public class LoginNetworkManager : NetworkManager
    {
        /**
         * Information about the login server sent to the client.
         */
        public class LoginServerMessage : MessageBase
        {
            // If null, then no 
            public string serverAddress;
        }

        /**
         * Information aboout the player's chosen character sent from client to server
         */
        public class CharacterSelectMessage : MessageBase
        {
            public CharacterResponse character;
        }

        [SerializeField] private string loginServerAddress = null;
        [SerializeField] private LoginManager loginManagerPrefab = null;

        private LoginManager loginManager;
        private bool hasLoginServer; // whether the login server is found and alive

        public override void Start()
        {
            base.Start();

            // TODO: Should this be called everywhere?

            // Set the defaults for the login manager
            var loginManagerGameObject = Instantiate(loginManagerPrefab);
            loginManager = loginManagerGameObject.GetComponent<LoginManager>();
            loginManager.UpdateApiAddress(loginServerAddress, character => SpawnPlayerWithLoginServer(NetworkServer.localConnection, character));
            loginManager.ApiHeartbeat(ConfirmLoginServer);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<CharacterSelectMessage>(OnCharacterSelectMessage);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // This doesn't require authentication, as we want the user to authenticate with the Login Server first (which we get sent)
            NetworkClient.RegisterHandler<LoginServerMessage>(OnLoginDataMessage, false);
        }

        private void ConfirmLoginServer(string response, bool apiAlive)
        {
            if (!apiAlive) {
                Debug.Log("Login server could not be reached.");
                hasLoginServer = false;
                return;
            }

            Debug.Log("Connection to Login server established.");
            hasLoginServer = true;
        }

        private void BeginLoginProcedure(string response, bool apiAlive)
        {
            if (!apiAlive) {
                Debug.LogError("Could not reach login server at login procedure start!");
                return;
            }

            loginManager.ShowLoginWindow();
        }

        /**
         * Step 1: When the player connects, the server sends info about the login server to the client.
         */
        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);
            // Must always send a message, so the client knows if they should spawn through the login server or not
            conn.Send(new LoginServerMessage() { serverAddress = hasLoginServer ? loginServerAddress : null });
        }

        /**
         * In the meantime, client just checks scene stuff.
         */
        public override void OnClientConnect(NetworkConnection conn)
        {
            if (clientLoadedScene) {
                // TODO: Mirror has a different workflow for clients when loading a separate scene after connecting to a server. If we need one, someone should implement it.
                Debug.LogWarning("The Login system does not support having a separate Online Scene yet!");
            }
        }

        /**
         * The client recieves the message informing them of the Login Server,
         * Uses this to start the player's character select process
         */
        private void OnLoginDataMessage(NetworkConnection conn, LoginServerMessage message)
        {
            if (message.serverAddress == null) {
                SpawnPlayerWithoutLoginServer(conn);
                return;
            }

            // Update our login server to match theirs. Attempt to connect.
            // If successful, hand over control to the LoginManager, telling it to call SpawnPlayerWithLoginServer when
            // the user has chosen their player.
            loginManager.UpdateApiAddress(message.serverAddress, character => SpawnPlayerWithLoginServer(conn, character));
            loginManager.ApiHeartbeat(BeginLoginProcedure);
        }

        /**
         * Once the player has selected a character, the client tells the server of the chosen character.
         */
        private void SpawnPlayerWithLoginServer(NetworkConnection conn, CharacterResponse characterResponse)
        {
            ClientScene.Ready(conn);
            conn.Send(new CharacterSelectMessage { character = characterResponse });
        }

        /**
         * If the client is told that the login server doesn't exist, they just signal that they are ready to start.
         */
        private void SpawnPlayerWithoutLoginServer(NetworkConnection conn)
        {
            ClientScene.Ready(conn);
            if (autoCreatePlayer) {
                ClientScene.AddPlayer();
            }
        }

        /**
         * If the client sends a CharacterSelect, we log them in with that character.
         */
        private void OnCharacterSelectMessage(NetworkConnection conn, CharacterSelectMessage characterSelection)
        {
            if (!IsPlayerPrefabValid(conn)) {
                return;
            }

            // If they somehow fuck up the 
            if (characterSelection?.character == null) {
                base.OnServerAddPlayer(conn);
                return;
            }

            // Spawn player based on their character choices
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.name = characterSelection.character.name;

            NetworkServer.AddPlayerForConnection(conn, player);
        }

        private bool IsPlayerPrefabValid(NetworkConnection conn)
        {
            if (playerPrefab == null) {
                Debug.LogError("The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
                return false;
            }

            if (playerPrefab.GetComponent<NetworkIdentity>() == null) {
                Debug.LogError("The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
                return false;
            }

            if (conn.identity != null) {
                Debug.LogError("There is already a player for this connections.");
                return false;
            }

            return true;
        }
    }
}