using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using MySql.Data.MySqlClient;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class UserDatabaseObject : MonoBehaviour
{
    public string ckey;

    private void Start()
    {
        UpdateCkey();
    }

    [ContextMenu("Update CKey")]
    public void UpdateCkey()
    {
        ckey = LoginNetworkManager.singleton.localCkey;
        LocalPlayerManager.singleton.ckey = ckey;
    }
    
    [ContextMenu("Save user")]
    public void Save()
    {
        DatabaseConnectionManager database = DatabaseConnectionManager.singleton;

        string sql = "INSERT INTO registeredUsers(ckey) VALUES ('" + ckey + "')";
        MySqlCommand cmd = new MySqlCommand(sql, database.conn);

        Debug.Log(cmd.ExecuteScalar());
    }
}
