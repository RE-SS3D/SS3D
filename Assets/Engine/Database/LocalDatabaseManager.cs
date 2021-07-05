using System.Collections;
using System.Data;
using Mirror;
using UnityEngine;

using MySql.Data.MySqlClient;
using Debug = UnityEngine.Debug;

namespace SS3D.Engine.Database
{
    /// <summary>
    /// <b>Server-side manager that handles the connection to the local database on the server.</b>
    ///
    /// <para>
    ///     There's an explanation for the whole process to configure the database
    /// in the file "database.sql", read it and then come back to this.
    /// </para>
    ///
    /// <para>
    /// This connects to the database and executes SQL queries,
    /// they should all be serverside.
    /// </para>
    /// </summary>
    public class LocalDatabaseManager : NetworkBehaviour
    {
        // LocalDatabaseManager is a singleton,
        // if you don't know what it is:
        // A singleton makes sure that the class has only one instance so everytime 
        // we do LocalDatabaseManager.singleton, we are getting the same guy
        public static LocalDatabaseManager singleton { get; private set; }

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
        // this is needed for the MySQL framework, no need to understand, it is our connection to the database
        private MySqlConnection conn;

        // User manager for the database
        public UserDatabaseObject userDatabaseObject;
        // Character data manager for the database
        public CharacterDatabaseObject characterDatabaseObject;
        
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

        [Server]
        private void Start()
        {
            // try to connect with the database
            // will throw a message if something goes wrong
            Connect();
        }
        
        /// <summary>
        ///  Server-side method that handles the database connection
        /// </summary>
        /// <returns>returns the connection state</returns>
        [Server]
        [ContextMenu("Connect")]
        public ConnectionState Connect()
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
            return GetDatabaseState(conn);
        }

        /// <summary>
        /// Checks the database state
        /// </summary>
        /// <param name="conn">SQL connection</param>
        /// <returns>SQL connection state</returns>
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

        /// <summary>
        /// <b>Coroutine to help GetConnectionState()</b>
        /// </summary>
        /// <param name="state">current database's connection state</param>
        public IEnumerator WaitUntilConnected(ConnectionState state)
        {
            // wait until we are not connecting
            yield return new WaitUntil(delegate { return state != ConnectionState.Connecting; });
        }
        
        /// <summary>
        /// <b>Server-side method that executes a SQL query in the local database</b>
        /// </summary>
        /// <param name="sql">SQL Query</param>
        /// <returns>Query result</returns>
        [Server]
        public object ExecuteQuery(string sql)
        {
            // we get the connection state to not try using a database that is offline
            ConnectionState state = GetDatabaseState(conn);
            
            Debug.Log("Executing new SQL query, database state: " + conn);

            if (state == ConnectionState.Closed || state == ConnectionState.Broken)
            {
                // if we are disconnected from the database we try connecting again
                // TODO: Not being able to connect after x tries check
                Connect();
            }
            
            // sets up the query for the MySQL library
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            // executes the query and gets the result
            object result = cmd.ExecuteScalar();

            // if theres a result we debug it, or just comment it out.
            // TODO: Improve logging feature
            if (result != null)
                return result;

            return null;
        }

    }
}
