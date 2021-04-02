using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    /// <summary>
    /// Handles user database operations.
    ///
    /// This should be used to register users into your server,
    /// when a new user joins, the database should know about it.
    ///
    /// This uses the DatabaseConnectionManager that is server-side to handle
    /// sql queries.
    /// 
    /// CKEY:
    ///     The client key should be unique, it will possibly eventually be
    ///     passed by our Hub, we don't know how yet, but that's how it should work,
    ///     once that is going, we will have out own database with that user's character data
    ///     so characters can be transported from server to server.
    ///     Also important for bans and admin permissions.
    /// </summary>
    public class UserDatabaseObject : NetworkBehaviour
    {
        // unique key to users, ckey stands for client key
        public string ckey;

        private void Start()
        {
            // this updates at start cause we  start this when we connect, so we check if this user was here once or not
            UpdateCkey();
        }
        
        [Client]
        [ContextMenu("Update CKey")]
        public void UpdateCkey()
        {
            // this is just a test, we have to have a way to work on the localCkey, probably with the Hub
            ckey = LoginNetworkManager.singleton.localCkey;
            // and we assign it to the local player manager which is just handy stuff
            LocalPlayerManager.singleton.ckey = ckey;
        }

        [Command(ignoreAuthority = true)]
        [ContextMenu("Save user")]
        public void CmdSaveUser(string ckey)
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;

            string sql = "INSERT INTO registeredUsers(ckey) VALUES ('" + ckey + "')";

            database.ExecuteQuery(sql);
        }

        public void SaveUser()
        {
            CmdSaveUser(ckey);
        }
    }
}
