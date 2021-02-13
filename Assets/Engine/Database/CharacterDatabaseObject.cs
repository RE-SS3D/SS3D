using Mirror;
using MySql.Data.MySqlClient;
using UnityEngine;

namespace SS3D.Engine.Database
{
    public class CharacterDatabaseObject : MonoBehaviour
    {
        public string name;
        // TODO: Job preferences

        [ContextMenu("Save character")]
        public void Save()
        {
            // TODO: Try getting data, if there's none, Save it otherwise, update it
        }

        public void SaveCharacterData()
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
            MySqlCommand cmd = new MySqlCommand(sql, database.conn);
            
            Debug.Log(cmd.ExecuteScalar()); 
        }
        
        [Command]
        [ContextMenu("Get Character Data")]
        public string GetCharacterData()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;
            string ckey = LocalPlayerManager.singleton.ckey;
            
            string sql = "SELECT * FROM CharacterData WHERE ckey = '" + ckey + "'";

            MySqlCommand cmd = new MySqlCommand(sql, database.conn);
            
            string result = cmd.ExecuteScalar().ToString();
            return result;
        }
    }
}