using Mirror;
using UnityEngine;

namespace SS3D.Engine.Database
{
    // Database Object of the character data
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
            
            database.ExecuteQuerry(sql);
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

            database.ExecuteQuerry(sql);
        }
    }
}
