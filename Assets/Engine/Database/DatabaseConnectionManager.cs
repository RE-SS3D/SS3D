using System.Collections;
using System.Data;
using Mirror;
using UnityEngine;

using MySql;
using MySql.Data.MySqlClient;
using Debug = UnityEngine.Debug;

namespace SS3D.Engine.Database
{
    // Handles the connection to the local database on the host
    public class DatabaseConnectionManager : NetworkBehaviour
    {
        public static DatabaseConnectionManager singleton { get; private set; }

        // database ip
        [SerializeField] private string serverURL;
        // database user
        [SerializeField] private string user;
        // database name
        [SerializeField] private string database;
        // database port
        [SerializeField] private string port;
        // database pasword
        [SerializeField] private string password;

        // full connection string
        private string connectionString;
        public MySqlConnection conn;

        private void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                singleton = this;
            }
        }

        private void Start()
        {
            Connect();
        }

        [Server]
        [ContextMenu("Connect")]
        // Handles the database connection
        public void Connect()
        {
            string connection =
                "server=" + serverURL + ";" +
                "user=" + user + ";" +
                "database=" + database + ";" +
                "port=" + port + ";" +
                "password=" + password;

            connectionString = connection;
            conn = new MySqlConnection(connection);
            // actually connects
            conn.Open();
            
            // just to make sure lets do this
            GetDatabaseState(conn);
        }

        public ConnectionState GetDatabaseState(MySqlConnection conn)
        {
            ConnectionState state = conn.State;
            
            // We wait until we have a state that is not "connecting"
            StartCoroutine(WaitUntilConnected(state));
            Debug.Log("connecting to database: " + connectionString);

            switch (state)
            {
                case ConnectionState.Open:
                    Debug.Log("connected");
                    break;
                case ConnectionState.Closed:
                    Debug.Log("failed to connect");
                    break;
                case ConnectionState.Broken:
                    Debug.Log("connection lost");
                    break;
                case ConnectionState.Executing:
                    Debug.Log("database is executing a command");
                    break;
                case ConnectionState.Fetching:
                    Debug.Log("database is fetching data");
                    break;
            }

            return state;
        }

        public IEnumerator WaitUntilConnected(ConnectionState state)
        {
            // wait until we are not connecting
            yield return new WaitUntil(delegate { return state != ConnectionState.Connecting; });
        }

        [Server]
        // handles any querry execution
        public void ExecuteQuerry(string sql)
        {
            // we get the connection state to not try using a database that is offline
            ConnectionState state = GetDatabaseState(conn);
            
            Debug.Log("Executing new SQL query, database state: " + conn);

            if (state == ConnectionState.Closed || state == ConnectionState.Broken)
            {
                // if we are disconnected from the database we try connecting again
                Connect();
            }
            
            // sets up the querry for the MySQL "framework"
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            // executes the querry and handles the result of the querry
            object result = cmd.ExecuteScalar();

            // if theres a result we debug it, or just comment it out. we should have a log for this eventually
            if (result != null)
                Debug.Log(cmd.ExecuteScalar().ToString());
        }

    }
}
