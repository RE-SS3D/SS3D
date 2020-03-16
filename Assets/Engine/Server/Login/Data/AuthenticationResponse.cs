using UnityEngine;

namespace SS3D.Engine.Server.Login.Data
{
    /// <summary>
    /// Data class to used to deserialize an authentication response from CentCom
    /// </summary>
    [System.Serializable]
     public class AuthenticationResponse
     {
         public string id;
         public string email;
         public string token;
 
         public static AuthenticationResponse CreateFromJSON(string jsonString)
         {
             return JsonUtility.FromJson<AuthenticationResponse>(jsonString);
         }
     }
}