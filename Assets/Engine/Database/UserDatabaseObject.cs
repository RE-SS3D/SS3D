using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    // Handles user database operations
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

            database.ExecuteQuerry(sql);
        }

        public void SaveUser()
        {
            CmdSaveUser(ckey);
        }
    }
}
