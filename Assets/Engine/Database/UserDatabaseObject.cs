using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    public class UserDatabaseObject : NetworkBehaviour
    {
        public string ckey;

        private void Start()
        {
            UpdateCkey();
        }
        
        [Client]
        [ContextMenu("Update CKey")]
        public void UpdateCkey()
        {
            ckey = LoginNetworkManager.singleton.localCkey;
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