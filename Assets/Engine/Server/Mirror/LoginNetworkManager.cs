    using System;
using System.Collections;
using UnityEngine;
using SS3D.Engine.Server.Login.Data;
using SS3D.Engine.Server.Login.Networking;
using SS3D.Engine.Server.Round;
using System.Net;

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
        // This is a server-only field. On the client it will mean nothing.
        //[SerializeField] bool useLoginSystemOnLocalHost;

        // Does the server require ingame login?
        [SerializeField] bool useLoginSystem;

        // Warmup time until round starts
        [Range(3, 3600)]
        [SerializeField] int warmupTime;
        /**
         * Information about the login server sent to the client.
         */
        public class LoginServerMessage : MessageBase
        {
            // If null, then no 
            public string serverAddress;
        }

        /**
         * Information about the player's chosen character sent from client to server
         */
        public class CharacterSelectMessage : MessageBase
        {
            public CharacterResponse character;
        }

        [SerializeField] private string loginServerAddress = null;
        [SerializeField] private GameObject playerDummyPrefab = null;
        [SerializeField] private LoginManager loginManagerPrefab = null;
        [SerializeField] private GameObject roundManagerPrefab = null;

        private LoginManager loginManager;
        public RoundManager roundManager;

        private bool hasLoginServer; // whether the login server is found and alive

        public override void Start()
        {
            base.Start();

            // TODO: Should this be called everywhere?

            // Set the defaults for the login manager
            if (useLoginSystem)
            {
                var loginManagerGameObject = Instantiate(loginManagerPrefab);
                loginManager = loginManagerGameObject.GetComponent<LoginManager>();
                loginManager.UpdateApiAddress(loginServerAddress,
                    character => SpawnPlayerWithLoginServer(NetworkServer.localConnection, character));
                loginManager.ApiHeartbeat(ConfirmLoginServer);
            }
        }

        /// <summary>
        /// Initial server setup
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<CharacterSelectMessage>(OnCharacterSelectMessage);
            SetupServerManagers();
        }

        /// <summary>
        /// Server setup after round restart
        /// </summary>
        /// <param name="sceneName"></param>
        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            SetupServerManagers();
            NetworkServer.SendToAll(new LoginServerMessage
                {serverAddress = hasLoginServer ? loginServerAddress : null});
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // This doesn't require authentication, as we want the user to authenticate with the Login Server first (which we get sent)
            NetworkClient.RegisterHandler<LoginServerMessage>(OnLoginDataMessage, false);
        }

        private void SetupServerManagers()
        {
            if (roundManager == null)
            {
                roundManager = Instantiate(roundManagerPrefab).GetComponent<RoundManager>();
            }

            NetworkServer.Spawn(roundManager.gameObject);
            roundManager = roundManager.GetComponent<RoundManager>();
            roundManager.SetWarmupTime(warmupTime);
            roundManager.StartWarmup();
        }

        private void ConfirmLoginServer(string response, bool apiAlive)
        {
            if (!apiAlive)
            {
                Debug.Log("Login server could not be reached.");
                hasLoginServer = false;
                return;
            }

            Debug.Log("Connection to Login server established.");
            hasLoginServer = true;
        }

        private void BeginLoginProcedure(string response, bool apiAlive)
        {
            if (!apiAlive)
            {
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

            bool userMustLogin = useLoginSystem && hasLoginServer;

            // Must always send a message, so the client knows if they should spawn through the login server or not
            conn.Send(new LoginServerMessage() {serverAddress = userMustLogin ? loginServerAddress : null});
        }

        /**
         * Once the client establishes a connection, it immediately tells the server to add them as a player
         */
        public override void OnClientConnect(NetworkConnection conn)
        {
            if (clientLoadedScene)
            {
                // TODO: Mirror has a different workflow for clients when loading a separate scene after connecting to a server. If we need one, someone should implement it.
                Debug.LogWarning("The Login system does not support having a separate Online Scene yet!");
                return;
            }

            ClientScene.AddPlayer(conn);
        }

        /**
         * When the server receives the add player command, it creates a dummy object of the player.
         * This is necessary so the client could begin receiving RPC calls.
         * The dummy player is replaced with the actual player in SpawnPlayerAfterRoundStart()
         */
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            GameObject player = Instantiate(playerDummyPrefab);
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        /**
         * The client receives the message informing them of the Login Server,
         * Uses this to start the player's character select process
         */
        private void OnLoginDataMessage(NetworkConnection conn, LoginServerMessage message)
        {
            if (message.serverAddress == null)
            {
                SpawnPlayerWithoutLoginServer(conn);
                return;
            }

            // Update our login server to match theirs. Attempt to connect.
            // If successful, hand over control to the LoginManager, telling it to call SpawnPlayerWithLoginServer when
            // the user has chosen their player.
            loginManager.UpdateApiAddress(message.serverAddress,
                character => SpawnPlayerWithLoginServer(conn, character));
            loginManager.ApiHeartbeat(BeginLoginProcedure);
        }

        /**
         * Once the player has selected a character, the client tells the server of the chosen character.
         */
        private void SpawnPlayerWithLoginServer(NetworkConnection conn, CharacterResponse characterResponse)
        {
            conn.Send(new CharacterSelectMessage {character = characterResponse});
        }

        /**
         * If the client is told that the login server doesn't exist, we build them a John Doe.
         */
        private void SpawnPlayerWithoutLoginServer(NetworkConnection conn)
        {
            CharacterResponse characterResponse = new CharacterResponse();
            characterResponse.name = "John Doe";
            conn.Send(new CharacterSelectMessage {character = characterResponse});
        }

        /**
         * If the client sends a CharacterSelect, we log them in with that character.
         */
        private void OnCharacterSelectMessage(NetworkConnection conn, CharacterSelectMessage characterSelection)
        {
            if (!IsPlayerPrefabValid(conn))
            {
                return;
            }

            StartCoroutine(SpawnPlayerAfterRoundStart(conn, characterSelection));
        }

        private IEnumerator SpawnPlayerAfterRoundStart(NetworkConnection conn, CharacterSelectMessage characterSelection)
        {
            // TODO: Should store players in an object until round is started, then spawn them all at once.
            yield return new WaitUntil(() => roundManager.IsRoundStarted);

            //Something has gone horribly wrong
            if (characterSelection?.character == null) throw new Exception("Could not read character data");

            // Spawn player based on their character choices
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.name = characterSelection.character.name;

            // NetworkServer.ReplacePlayerForConnection(conn, player);
            //Destroy dummy player
            NetworkServer.DestroyPlayerForConnection(conn);
            //Spawn actual player
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        private bool IsPlayerPrefabValid(NetworkConnection conn)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
                return false;
            }

            if (playerPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError(
                    "The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
                return false;
            }

            if (conn.identity != null && conn.identity.name != $"{playerDummyPrefab.name}(Clone)")
            {
                Debug.LogError("There is already a player for this connections.");
                return false;
            }

            return true;
        }
    }
}