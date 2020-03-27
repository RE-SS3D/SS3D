using UnityEngine;

namespace SS3D.Engine.Server.Login.Data
{
    /// <summary>
    /// Data class to used to deserialize an error message response from CentCom
    /// </summary>
    [System.Serializable]
    public class MessageResponse
    {
        public string message;

        public static MessageResponse CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<MessageResponse>(jsonString);
        }
    }
}