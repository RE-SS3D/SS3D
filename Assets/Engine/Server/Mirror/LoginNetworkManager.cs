﻿using System;
using System.Collections;
using Mirror;
using SS3D.Engine.Server.Login.Data;
using SS3D.Engine.Server.Login.Networking;
using SS3D.Engine.Server.Round;
using UnityEngine;

namespace SS3D.Engine.Server.Mirror
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
        public static LoginNetworkManager LoginSingleton { get; private set; }

        // Allows for updating the server numbers when players connect / disconnect.
        public event System.Action ClientNumbersUpdated;

        // Warmup time until round starts
        [Range(3, 3600)]
        [SerializeField] int warmupTime;
        /**
         * Information about the login server sent to the client.
         */
        private struct LoginServerMessage : NetworkMessage
        {
            // If null, then no 
            public string ServerAddress;
        }

        /**
         * Information about the player's chosen character sent from client to server
         */
        public struct CharacterSelectMessage : NetworkMessage
        {
            public CharacterResponse Character;
        }

        // LOGIN STUFF
        private bool hasLoginServer; // whether the login server is found and alive
        [SerializeField] private string loginServerAddress = null;
        
        [SerializeField] private GameObject soulPrefab = null;
        
        [SerializeField] private LoginManager loginManagerPrefab = null;
        [SerializeField] private GameObject roundManagerPrefab = null;
        
        
        // Does the server require ingame login?
        [SerializeField] bool useLoginSystem;
        
        private LoginManager loginManager;      
        public RoundManager roundManager;

        // Loading screen object, perhaps might be removed later
        [SerializeField] private GameObject loadingScreen;
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

        public override void Awake()
        {
            base.Awake();
            InitializeSingleton();
        }
        
        bool InitializeSingleton()
        {
            if (LoginSingleton != null && LoginSingleton == this) return true;
            
            if (dontDestroyOnLoad)
            {
                if (LoginSingleton != null)
                {
                    Debug.LogWarning("Multiple NetworkManagers detected in the scene. Only one NetworkManager can exist at a time. The duplicate NetworkManager will be destroyed.");
                    Destroy(gameObject);

                    // Return false to not allow collision-destroyed second instance to continue.
                    return false;
                }
                Debug.Log("NetworkManager created singleton (DontDestroyOnLoad)");
                LoginSingleton = this;
                if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.Log("NetworkManager created singleton (ForScene)");
                LoginSingleton = this;
            }

            // set active transport AFTER setting singleton.
            // so only if we didn't destroy ourselves.
            Transport.activeTransport = transport;

            return true;
        }

        /// <summary>
        /// Initial server setup
        /// </summary>
        [Server]
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
        [Server]
        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            SetupServerManagers();
            NetworkServer.SendToAll(new LoginServerMessage
                {ServerAddress = hasLoginServer ? loginServerAddress : null});
            
            UpdateLoadingScreen(false);
        }

        [Client]
        public override void OnStartClient()
        {
            base.OnStartClient();
            // This doesn't require authentication, as we want the user to authenticate with the Login Server first (which we get sent)
            NetworkClient.RegisterHandler<LoginServerMessage>(OnLoginDataMessage, false);
        }

        [Server]
        private void SetupServerManagers()
        {
            roundManager = GameObject.FindObjectOfType<RoundManager>();
            if (roundManager == null)
            {
                roundManager = Instantiate(roundManagerPrefab).GetComponent<RoundManager>();
            }

            NetworkServer.Spawn(roundManager.gameObject);
            roundManager = roundManager.GetComponent<RoundManager>();
            roundManager.SetWarmupTime(warmupTime);
            //roundManager.StartWarmup();
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
        [Server]
        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);

            bool userMustLogin = useLoginSystem && hasLoginServer;

            // Must always send a message, so the client knows if they should spawn through the login server or not
            conn.Send(new LoginServerMessage() {ServerAddress = userMustLogin ? loginServerAddress : null});

            // Ensure the display gets updated.
            StartCoroutine(UpdatePlayerCountDelayed());
        }

        /**
         * Once the client establishes a connection, it immediately tells the server to add them as a player
         */
        [Client]
        public override void OnClientConnect(NetworkConnection conn)
        {
            if (clientLoadedScene)
            {
                // TODO: Mirror has a different workflow for clients when loading a separate scene after connecting to a server. If we need one, someone should implement it.
                Debug.LogWarning("The Login system does not support having a separate Online Scene yet!");
                return;
            }

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            NetworkClient.AddPlayer();
        }

        /**
         * When the server receives the add player command, it creates a dummy object of the player.
         * This is necessary so the client could begin receiving RPC calls.
         * The dummy player is replaced with the actual player in SpawnPlayerAfterRoundStart()
         */
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Debug.Log("OnServerAddPlayer");

            if (!SceneLoaderManager.singleton.CommencedLoadingMap)
            {
                // There is no map loaded. This is the default condition.
                GameObject soul = Instantiate(soulPrefab);
                NetworkServer.AddPlayerForConnection(conn, soul);
                //GameObject player = Instantiate(playerDummyPrefab);
                //NetworkServer.AddPlayerForConnection(conn, player);
            }
            else
            {
                // A map has already been loaded when the client joins.
                StartCoroutine(OnServerAddPlayerDelayed(conn));
            }
        }

        private IEnumerator OnServerAddPlayerDelayed(NetworkConnection conn)
        {
            // Wait until the server has loaded the scene itself.
            while (!SceneLoaderManager.singleton.IsSelectedMapLoaded())
                yield return null;

            // Let the client know that the server has loaded the map.
            SceneLoaderManager.singleton.TargetInvokeMapLoaded(conn);

            // Wait for that to go through
            yield return new WaitForEndOfFrame();

            // Send client message to load the scene.
            conn.Send(SceneLoaderManager.singleton.GenerateSceneMessage());

            // Wait for that to go through
            yield return new WaitForEndOfFrame();

            // Now make the soul.
            GameObject soul = Instantiate(soulPrefab);
            NetworkServer.AddPlayerForConnection(conn, soul);

            // Wait for that to go through
            yield return new WaitForEndOfFrame();
            // Let the client know that the server has loaded the map.
            SceneLoaderManager.singleton.TargetSetActiveScene(conn);
        }



        /**
         * The client receives the message informing them of the Login Server,
         * Uses this to start the player's character select process
         */
        [Client]
        private void OnLoginDataMessage(LoginServerMessage message)
        {
            if (message.ServerAddress == null)
            {
                SpawnPlayerWithoutLoginServer(NetworkClient.connection);
                return;
            }

            // Update our login server to match theirs. Attempt to connect.
            // If successful, hand over control to the LoginManager, telling it to call SpawnPlayerWithLoginServer when
            // the user has chosen their player.
            loginManager.UpdateApiAddress(message.ServerAddress,
                character => SpawnPlayerWithLoginServer(NetworkClient.connection, character));
            loginManager.ApiHeartbeat(BeginLoginProcedure);
        }

        /**
         * Once the player has selected a character, the client tells the server of the chosen character.
         */
        [Client]
        private void SpawnPlayerWithLoginServer(NetworkConnection conn, CharacterResponse characterResponse)
        {
            conn.Send(new CharacterSelectMessage {Character = characterResponse});
        }

        /**
         * If the client is told that the login server doesn't exist, we build them a John Doe.
         */
        [Client]
        private CharacterResponse SpawnPlayerWithoutLoginServer(NetworkConnection conn)
        {
            CharacterResponse characterResponse = GetDefaultCharacter();
            conn.Send(new CharacterSelectMessage {Character = characterResponse});
            return characterResponse;
        }

        /**
         * If the client sends a CharacterSelect, we log them in with that character.
         */
        [Server]
        private void OnCharacterSelectMessage(NetworkConnection conn, CharacterSelectMessage characterSelection)
        {
            if (!IsPlayerPrefabValid(conn))
            {
                return;
            }

            //StartCoroutine(SpawnPlayerAfterRoundStart(conn, characterSelection));
        }
      
        /// <summary>
        /// Returns a character with the default configuration. This can be called on client and server.
        /// </summary>
        /// <returns></returns>
        private CharacterResponse GetDefaultCharacter()
        {
            CharacterResponse character = new CharacterResponse();
            character.name = "John Doe";
            return character;
        }


        [Server]
        public void SpawnPlayerAfterRoundStart(NetworkConnection conn)
        {

            CharacterResponse character = GetDefaultCharacter();

            Debug.Log("Spawning player after round start " + "conn: " + conn.address + " character: " + character.name);
            //Something has gone horribly wrong
            if (character == null) throw new Exception("Could not read character data");

            // Spawn player based on their character choices
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.name = character.name;

            if (NetworkClient.connection.identity != null)
            {
                NetworkServer.DestroyPlayerForConnection(conn);
            }
            //Destroy dummy player
            //NetworkServer.DestroyPlayerForConnection(conn);
            
            //Spawn actual player
            NetworkServer.AddPlayerForConnection(conn, player);   
        }
        
        public IEnumerator SpawnPlayerAfterRoundStart(NetworkConnection conn, CharacterSelectMessage characterSelection)
        {
            // TODO: Should store players in an object until round is started, then spawn them all at once.
            yield return new WaitUntil(() => roundManager.IsRoundStarted);

            //Something has gone horribly wrong
            if (characterSelection.Character == null) throw new Exception("Could not read character data");

            // Spawn player based on their character choices
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.name = characterSelection.Character.name;
            
            // NetworkServer.ReplacePlayerForConnection(conn, player);
            //Destroy dummy player
            NetworkServer.DestroyPlayerForConnection(conn);
            //Spawn actual player
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        [Server]
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

            if (conn.identity != null)
            {
                Debug.LogError("There is already a player for this connections.");
                return false;
            }

            return true;
        }
        
        [Client]
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
        {
            base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);

            UpdateLoadingScreen(true);
        }

        [Server]
        public override void OnServerChangeScene(string newSceneName)
        {
            base.OnServerChangeScene(newSceneName);
            
            UpdateLoadingScreen(true);
        }
        
        [Client]
        public override void OnClientSceneChanged(NetworkConnection conn)
        {
            base.OnClientSceneChanged(conn);
            
            UpdateLoadingScreen(false);
        }

        private void UpdateLoadingScreen(bool state)
        {
            if (loadingScreen != null)
                loadingScreen?.SetActive(state);
        }

        [Server]
        // Ensures that as clients disconnect, the client numbers are updated.
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            ClientNumbersUpdated?.Invoke();

        }

        [Server]
        private IEnumerator UpdatePlayerCountDelayed()
        {
            // Wait until the end of frame (so that the event has been subscribed to)
            yield return new WaitForEndOfFrame();

            // Fire the event
            ClientNumbersUpdated?.Invoke();

        }

    }
}