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

        [ContextMenu("Save character")]
        public void Save()
        {
            // TODO: Try getting data, if there's none, Save it otherwise, update it
        }

        [Command(ignoreAuthority = true)]
        public void CmdSaveCharacterData()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
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
            
            string sql = "SELECT * FROM CharacterData WHERE ckey = '" + ckey + "'";

            database.ExecuteQuerry(sql);
        }
    }
}
