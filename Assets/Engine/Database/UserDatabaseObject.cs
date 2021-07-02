using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    /// <summary>
    /// <para>
    /// <b>Handles user database operations.</b>
    /// </para>
    ///
    /// <para>
    /// This should be used to register users into your server,
    /// when a new user joins, the database should know about it.
    /// </para>
    ///
    /// <para>
    /// This uses the LocalDatabaseManager, which is a server-side class to handle
    /// local server's sql queries.
    /// </para>
    ///
    /// <param name="CKEY">
    ///         A client key that is unique to each user, it will possibly eventually be
    ///     passed by our Hub, we don't know how yet, but that's how it should work,
    ///     once that is going, we will have own database with that user's character data
    ///     so characters can be transported from server to server.
    ///     Also important for bans and admin permissions, yada yada yada.
    /// </param>
    /// </summary>
    public class UserDatabaseObject : NetworkBehaviour
    {
        // unique key to users, ckey stands for client key
        public string ckey;

        private void Start()
        {
            // Tells the server this CKEY has joined it
            UpdateCkey();
        }
        
        /// <summary>
        /// Updates the CKEY of the user on the LocalPlayerManager
        /// </summary>
        [Client]
        [ContextMenu("Update CKey")]
        public void UpdateCkey()
        {
            //TODO: find a better way to get the player CKEY
            
            // this is just a test, we have to have a way to work on the localCkey, probably with the Hub
            // because this is very unsecure
            ckey = LoginNetworkManager.singleton.localCkey;
            
            // we assign it to the local player manager which is just handy stuff
            LocalPlayerManager.singleton.ckey = ckey;
        }
        
        /// <summary>
        /// Saves user's ckey in the local database
        /// </summary>
        /// <returns></returns>
        [Server]
        [ContextMenu("Save user")]
        public void SaveUser(string ckey, NetworkConnectionToClient sender = null)
        {
            LocalDatabaseManager database = LocalDatabaseManager.singleton;
            
            // SQL query that inserts the CKEY in the database, as its a primary key it only inserts the first time
            string sql = "INSERT INTO registeredUsers(ckey) VALUES ('" + ckey + "')";

            database.ExecuteQuery(sql);
        }
    }
}
