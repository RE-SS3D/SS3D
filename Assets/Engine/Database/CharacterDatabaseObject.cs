using Mirror;
using UnityEngine;
#pragma warning disable 414

namespace SS3D.Engine.Database
{
    /// <summary>
    /// <para>
    /// <b>Database object for the character data</b>
    /// </para>
    /// 
    /// <para>
    /// This should contain and manage:
    ///         Selected character index, the character preferences
    ///         and the Database SQL commands that we use to save them.
    /// </para>
    ///
    /// <para>
    /// How:
    ///         Once we have a decent database configuration we will be able
    ///     to have proper character saving, with many characters for a player
    ///     to be able to save and load.
    /// </para>
    ///
    /// <para>
    /// Important for the database:
    ///         Each character should have his own job preferences, this is not
    ///     included in BYOND's SS13 (I think) but I think its important
    ///     to have.
    ///         Also having global job preferences could be nice for those who
    ///     don't care.
    /// </para>
    ///
    /// <param name="selectedCharacterIndex">Selected character's index, useful for the database and in-game menus.</param>
    /// <param name="characterData">Manages the character's job and antagonist preferences, aswell as for
    /// clothing, skin color, gender, hair yada yada. Everything related to character options.
    /// </param>
    /// <para>
    /// Developer note:
    ///     Entire JSONS are possible to send via database, so maybe try that for the preferences
    /// </para>
    /// </summary>
    public class CharacterDatabaseObject : NetworkBehaviour
    {
        // TODO: Job preferences and character stuff in a characterData class
        public int selectedCharacterIndex = 0;
        public CharacterData characterData;
        
        /// <summary>
        /// <b>Saves the data from the user's game into the server's local database</b>
        /// </summary>
        /// <param name="sender">connection that asked this Command, Mirror handles it for you</param>
        [Command(ignoreAuthority = true)]
        public void CmdSaveCharacterData(NetworkConnectionToClient sender = null)
        {
            LocalDatabaseManager database = LocalDatabaseManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
            // SQL Query to save a character
            string sql = $"INSERT INTO CharacterData(ckey, index) VALUES ('{ckey}', '{name}')";
            
            database.ExecuteQuery(sql);
        }
        
        /// <summary>
        /// <b>Gets all the character data for that user from the local database</b>
        /// <para>
        /// Processes the data from a SQL query into the a CharacterData list
        /// </para>
        /// </summary>
        /// <returns>all of user's character data</returns>
        [Server]
        [ContextMenu("Get Character Data")]
        public object GetAllCharacterData()
        {
            LocalDatabaseManager database = LocalDatabaseManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
            // SQL Querry to select all the character data from that CKEY, I should make a "selected character index" though
            // TODO: Update this to put a selected character id
            string sql = "SELECT * FROM CharacterData WHERE ckey = '" + ckey + "'";

            CharacterData[] characterData = null;
            var result = database.ExecuteQuery(sql);
            return result;
        }
    }
}
