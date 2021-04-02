using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    /// <summary>
    /// Database object for the character data
    /// 
    /// This should contain and manage:
    ///     selected character index, the current selected character's name,
    ///     the character job preferences, the character antag preferences and
    ///     the Database SQL commands that we use to save them.
    ///
    /// How:
    ///     Once we have a decent database configuration we will be able
    ///     to have proper character saving, with many characters for a player
    ///     to be able to save and load.
    /// 
    /// Important for the database:
    ///     Each character should have his own job preferences, this is not
    ///     included in BYOND's SS13 (I think) but I think its important
    ///     to have.
    /// </summary>
    public class CharacterDatabaseObject : NetworkBehaviour
    {
        // Character name
        public string name;
        // TODO: Job preferences
        // TODO: Selected characeter index 
        //int selectedCharacterIndex = 0;
        
        // Save character in the local database
        // [Server]
        [ContextMenu("Save character")]
        public void Save()
        {
            // TODO: Try getting data, if there's none, Save it otherwise, update it
            
            // Put the save stuff here idiot youre running it locally thats why its not working
        }

        // Send the save operation to the Server
        // TODO: Make this work
        [Command(ignoreAuthority = true)]
        public void CmdSaveCharacterData()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
            // SQL Querry to save a character
            string sql = "INSERT INTO CharacterData(ckey, name)" +
                         " VALUES " +
                         "('" + 
                         ckey + 
                         "', '" +
                         name +
                         "')";
            
            database.ExecuteQuery(sql);
        }
        
        [Command(ignoreAuthority = true)]
        [ContextMenu("Get Character Data")]
        public void CmdGetCharacterData()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
            // SQL Querry to select all the character data from that CKEY, I should make a "selected character index" though
            // TODO: Update this to put a selected character id
            string sql = "SELECT * FROM CharacterData WHERE ckey = '" + ckey + "'";

            database.ExecuteQuery(sql);
        }
    }
}
