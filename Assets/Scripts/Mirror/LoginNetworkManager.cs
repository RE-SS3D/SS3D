using Login;
using Login.Data;
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
        [SerializeField] private string loginServerAddress;
        [SerializeField] private LoginManager loginManagerPrefab;

        private LoginManager loginManager;
        private bool hasLoginServer;
        private readonly string noServerConstant = "NO_SERVER";

        private void Start()
        {
            base.Start();

            var loginManagerGameObject = Instantiate(loginManagerPrefab);
            loginManager = loginManagerGameObject.GetComponent<LoginManager>();
            loginManager.UpdateApiAddress(loginServerAddress, NetworkServer.localConnection, SpawnPlayerWithLoginServer);
            loginManager.ApiHeartbeat(ConfirmLoginServer);
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

        public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage extraMessage)
        {
            if (!IsPlayerPrefabValid(conn))
            {
                return;
            }

            if (extraMessage?.value == null)
            {
                base.OnServerAddPlayer(conn, null);
                return;
            }
            
            /*
             * Deserialize character data from byte[]
             * Format controlled in SpawnPlayerWithLoginServer()
             * Current expected format:
             * {
             *     "name" : "SomePlayerName"
             * }
             */
            string rawJson = System.Text.Encoding.UTF8.GetString(extraMessage.value);
            string name = CharacterResponse.CreateFromJSON(rawJson).name;
        
            //Spawn player based on their character choices
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.name = name;

            NetworkServer.AddPlayerForConnection(conn, player);
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            //Must always send a message, so the client knows if they should spawn through the login server or not
            LoginServerMessage message = new LoginServerMessage() { serverAddress = noServerConstant};
            if (hasLoginServer)
            {
                message.serverAddress = loginServerAddress;
            }
            
            conn.Send(message);
        }
    
        public override void OnClientConnect(NetworkConnection conn)
        {
            if (clientLoadedScene)
            {
                //TODO: Mirror has a different workflow for clients when loading a separate scene after connecting to a server. If we need one, someone should implement it.
                Debug.LogWarning("The Login system does not support having a separate Online Scene yet!");
            }
        }
    
        public override void OnLoginDataMessage(NetworkConnection conn, LoginServerMessage message)
        {
            if (message.serverAddress == noServerConstant)
            {
                SpawnPlayerWithoutLoginServer(conn);
                return;
            }

            loginManager.UpdateApiAddress(message.serverAddress, conn, SpawnPlayerWithLoginServer);
            loginManager.ApiHeartbeat(BeginLoginProcedure);
        }

        private void SpawnPlayerWithLoginServer(NetworkConnection conn, CharacterResponse characterResponse)
        {
            ClientScene.Ready(conn);
            string json = JsonUtility.ToJson(characterResponse);
            
            //Convert to byte[] and send
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            ClientScene.AddPlayer(conn, data);
        }

        private void SpawnPlayerWithoutLoginServer(NetworkConnection conn)
        {
            ClientScene.Ready(conn);
            if (autoCreatePlayer)
            {
                ClientScene.AddPlayer();
            }
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
                Debug.LogError("The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
                return false;
            }

            if (conn.playerController != null)
            {
                Debug.LogError("There is already a player for this connections.");
                return false;
            }

            return true;
        }
    }
}
