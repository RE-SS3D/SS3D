using System.Collections;
using System.Data;
using Mirror;
using UnityEngine;

using MySql;
using MySql.Data.MySqlClient;
using Debug = UnityEngine.Debug;

namespace SS3D.Engine.Database
{
    public class DatabaseConnectionManager : NetworkBehaviour
    {
        public static DatabaseConnectionManager singleton { get; private set; }

        [SerializeField] private string serverURL;
        [SerializeField] private string user;
        [SerializeField] private string database;
        [SerializeField] private string port;
        [SerializeField] private string password;

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
            conn.Open();

            GetDatabaseState(conn);
        }

        public ConnectionState GetDatabaseState(MySqlConnection conn)
        {
            ConnectionState state = conn.State;

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
            yield return new WaitUntil(delegate { return state != ConnectionState.Connecting; });
        }

        [Server]
        public void ExecuteQuerry(string sql)
        {
            ConnectionState state = GetDatabaseState(conn);
            
            Debug.Log("Executing new SQL query, database state: " + conn);

            if (state == ConnectionState.Closed || state == ConnectionState.Broken)
            {
                Connect();
            }
            
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            object result = cmd.ExecuteScalar();

            if (result != null)
                Debug.Log(cmd.ExecuteScalar().ToString());
        }

    }
}