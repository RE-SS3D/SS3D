using UnityEngine;

namespace SS3D.Engine.Server.Login.Data
{
    /// <summary>
    /// Data class to used serialize a users login request to CentCom
    /// </summary>
    [System.Serializable]
    public class CredentialRequest
    {
        public string email;
        public string password;

        public static CredentialRequest CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<CredentialRequest>(jsonString);
        }
    }
}