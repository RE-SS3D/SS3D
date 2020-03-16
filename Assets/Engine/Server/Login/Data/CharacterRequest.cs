using UnityEngine;

namespace SS3D.Engine.Server.Login.Data
{
    /// <summary>
    /// Data class to used serialize a character data request to CentCom
    /// </summary>
    [System.Serializable]
    public class CharacterRequest
    {
        public string name;

        public static CharacterRequest CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<CharacterRequest>(jsonString);
        }
    }
}