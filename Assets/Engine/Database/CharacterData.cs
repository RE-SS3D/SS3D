using MySql.Data.MySqlClient;
using UnityEngine;

namespace SS3D.Engine.Database
{
    public class CharacterData : MonoBehaviour
    {
        public string name;
        public string gender;

        [ContextMenu("Save character")]
        public void Save()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;

            string sql = "INSERT INTO CharacterData(name, gender)" +
                         " VALUES " +
                         "('" + 
                         name + 
                         "', '" +
                         gender +
                         "')";
            MySqlCommand cmd = new MySqlCommand(sql, database.conn);
            
            Debug.Log(cmd.ExecuteScalar()); 
        }
        
        public void Save(string name, string gender)
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;

             string sql = "INSERT INTO CharacterData(name, gender)" +
                         " VALUES ('" + 
                         name + "', '" +
                         gender +
                         "')";
            MySqlCommand cmd = new MySqlCommand(sql, database.conn);
            
            Debug.Log(cmd.ExecuteScalar());
        }

        [ContextMenu("Get Character Data")]
        public void GetCharacterData()
        {
            DatabaseConnectionManager database = DatabaseConnectionManager.singleton;

            string sql = "SELECT * FROM CharacterData";

            MySqlCommand cmd = new MySqlCommand(sql, database.conn);
            
            Debug.Log(cmd.ExecuteScalar());
        }
    }
}