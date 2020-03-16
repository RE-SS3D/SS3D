using UnityEngine;

namespace SS3D.Engine.Server.Login.Data
{
    /// <summary>
    /// Data class to used to deserialize a character data response from CentCom
    /// </summary>
    [System.Serializable]
    //TODO: consider storing other customisation options in a separate serializable object, so they can all be custom format json in the DB
    public class CharacterResponse
    {
        public string id;
        public string name;
        public string userId;

        public static CharacterResponse CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<CharacterResponse>(jsonString);
        }
    }
}