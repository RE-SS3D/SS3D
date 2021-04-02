using System.Collections;
using System.Data;
using Mirror;
using UnityEngine;

using MySql;
using MySql.Data.MySqlClient;
using Debug = UnityEngine.Debug;

namespace SS3D.Engine.Database
{
    /// <summary>
    /// Handles the connection to the local database on the server.
    ///
    /// There's an explanation for the whole process to configure the database
    /// in the file "database.sql", read it and then come back to this.
    ///
    /// This connects to the database and executes SQL queries,
    /// they should all be serverside.
    /// </summary>
    public class DatabaseConnectionManager : NetworkBehaviour
    {
        // Our DatabaseConnectionManager should be a singleton,
        // if you don't know what it is, its basically
        // making sure there is only one of its kind
        // and that we can easily find him via code
        public static DatabaseConnectionManager singleton { get; private set; }

        // database ip
        [SerializeField] private string serverURL;
        // database user
        [SerializeField] private string user;
        // database name
        [SerializeField] private string database;
        // database port
        [SerializeField] private string port;
        // database password
        [SerializeField] private string password;

        // full connection string
        private string connectionString;
        // this is needed for the MySQL framework, no need to understand,it is our connection to the database
        public MySqlConnection conn;

        // Configures the singleton
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

        // At start we try to connect with the database
        // will throw a message if something goes wrong
        private void Start()
        {
            Connect();
        }
        
        // Handles the database connection
        [Server]
        [ContextMenu("Connect")]
        public void Connect()
        {
            // this creates the connection string to make the connection to the database with all the info we need
            string connection =
                "server=" + serverURL + ";" +
                "user=" + user + ";" +
                "database=" + database + ";" +
                "port=" + port + ";" +
                "password=" + password;

            // we save it just in case we need it
            connectionString = connection;
            // we create the connection object
            conn = new MySqlConnection(connection);
            // actually connects
            conn.Open();
            
            // checks if we are connected
            GetDatabaseState(conn);
        }

        // This checks the connection to the database
        // we try to connect for a few seconds
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

        // Coroutine to help GetConnectionState()
        public IEnumerator WaitUntilConnected(ConnectionState state)
        {
            // wait until we are not connecting
            yield return new WaitUntil(delegate { return state != ConnectionState.Connecting; });
        }

        // handles any query execution
        [Server]
        public void ExecuteQuery(string sql)
        {
            // we get the connection state to not try using a database that is offline
            ConnectionState state = GetDatabaseState(conn);
            
            Debug.Log("Executing new SQL query, database state: " + conn);

            if (state == ConnectionState.Closed || state == ConnectionState.Broken)
            {
                // if we are disconnected from the database we try connecting again
                Connect();
            }
            
            // sets up the query for the MySQL framework
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            // executes the query and handles the result
            object result = cmd.ExecuteScalar();

            // if theres a result we debug it, or just comment it out. we should have a log for this eventually
            if (result != null)
                Debug.Log(cmd.ExecuteScalar().ToString());
        }

    }
}
