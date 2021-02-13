using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using UnityEngine;

using MySql;
using MySql.Data.MySqlClient;
using Debug = UnityEngine.Debug;

public class DatabaseConnectionManager : MonoBehaviour
{
    public static DatabaseConnectionManager singleton { get; private set; }
    
    [SerializeField] private string serverURL;
    [SerializeField] private string user;
    [SerializeField] private string database;
    [SerializeField] private string port;
    [SerializeField] private string password;

    private string connectionString;

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
        MySqlConnection conn = new MySqlConnection(connection);
        conn.Open();
        
        GetDatabaseState(conn);
    }

    public ConnectionState GetDatabaseState(MySqlConnection conn)
    {
        ConnectionState state = conn.State;

        StartCoroutine(WaitUntilConnected(state));
        Debug.Log("connecting to database: " + connectionString);

        switch(state)
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
    
}
