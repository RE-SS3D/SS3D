using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SS3D.Engine.Server.Login.Data;

namespace SS3D.Engine.Server.Login.Networking
{
    /// <summary>
    /// Class responsible for communicating with the Login API - CentCom.
    /// Contains all supported endpoints and handling for initiating request coroutines
    /// </summary>
    public class LoginServerClient
    {
        public static readonly string prefix = "/api";

        //Method_EndpointName
        //Anonymous requests
        private static readonly string Get_Heartbeat = prefix + "/heartbeat";
        private static readonly string Post_Register = prefix + "/user/register";
        private static readonly string Post_Authenticate = prefix + "/user/authenticate";

        //Authenticated requests
        private static readonly string Get_CharacterList = prefix + "/character/all";
        private static readonly string Post_CreateCharacter = prefix + "/character/create";
        private static readonly string Delete_DeleteCharacter = prefix + "/character";

        private string connectionAddress;
        private MonoBehaviour monoBehaviour;
        private string token;

        public LoginServerClient(string connectionAddress, MonoBehaviour monoBehaviour)
        {
            this.connectionAddress = connectionAddress;
            this.monoBehaviour = monoBehaviour;
        }

        /// <summary>
        /// Store the authentication token received from CentCom. Will be used in future requests that require authentication.
        /// </summary>
        /// <param name="token">JWT</param>
        public void StoreToken(string token)
        {
            this.token = token;
        }

        /// <summary>
        /// CentCom call to determine if it is alive.
        /// </summary>
        /// <param name="resultCallback"></param>
        public void Heartbeat(Action<string, bool> resultCallback)
        {
            monoBehaviour.StartCoroutine(HandleSimpleRequest(
                UnityWebRequest.kHttpVerbGET,
                connectionAddress + Get_Heartbeat, 
                "Heartbeat", 
                false,
                resultCallback
            ));
        }

        /// <summary>
        /// CentCom call to register a new user.
        /// </summary>
        /// <param name="credentialRequest"></param>
        /// <param name="resultCallback"></param>
        public void Register(CredentialRequest credentialRequest, Action<string, bool> resultCallback)
        {
            monoBehaviour.StartCoroutine(HandlePayloadRequest(
                UnityWebRequest.kHttpVerbPOST,
                connectionAddress + Post_Register, 
                JsonUtility.ToJson(credentialRequest),
                "Register", 
                false,
                resultCallback
            ));
        }

        /// <summary>
        /// CentCom call to log in and receive an authentication token.
        /// </summary>
        /// <param name="credentialRequest"></param>
        /// <param name="resultCallback"></param>
        public void Authenticate(CredentialRequest credentialRequest,  Action<string, bool> resultCallback)
        {
            monoBehaviour.StartCoroutine(HandlePayloadRequest(
                UnityWebRequest.kHttpVerbPOST,
                connectionAddress + Post_Authenticate, 
                JsonUtility.ToJson(credentialRequest),
                "Authenticate", 
                false,
                resultCallback
            ));
        }

        /// <summary>
        /// CentCom call to get all characters of the current user.
        /// Requires Authenticate to have been called at least once before.
        /// </summary>
        /// <param name="resultCallback"></param>
        public void GetCharacters(Action<string, bool> resultCallback)
        {
            ValidateToken("Get Characters");
            monoBehaviour.StartCoroutine(HandleSimpleRequest(
                UnityWebRequest.kHttpVerbGET,
                connectionAddress + Get_CharacterList, 
                "Get Characters", 
                true,
                resultCallback
            ));
        }

        /// <summary>
        /// CentCom call to store a new character for the current user.
        /// Requires Authenticate to have been called at least once before.
        /// </summary>
        /// <param name="characterRequest"></param>
        /// <param name="resultCallback"></param>
        public void SaveCharacter(CharacterRequest characterRequest, Action<string, bool> resultCallback) 
        {
            ValidateToken("Save Character");
            monoBehaviour.StartCoroutine(HandlePayloadRequest(
                UnityWebRequest.kHttpVerbPOST,
                connectionAddress + Post_CreateCharacter,
                JsonUtility.ToJson(characterRequest),
                "Save Character", 
                true,
                resultCallback
            ));
        }

        /// <summary>
        /// CentCom call to delete a character of the current user.
        /// Requires Authenticate to have been called at least once before.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="resultCallback"></param>
        public void DeleteCharacter(string id, Action<string, bool> resultCallback)
        {
            ValidateToken("Delete Character");
            monoBehaviour.StartCoroutine(HandleSimpleRequest(
                UnityWebRequest.kHttpVerbDELETE,
                connectionAddress + Delete_DeleteCharacter + $"/{id}",
                "Delete Character", 
                true,
                resultCallback
            ));
        }

        /// <summary>
        /// Handles requests that do not contain a data payload
        /// </summary>
        /// <param name="method">A value from UnityWebRequest.kHttpVerb<...></param>
        /// <param name="url">The endpoint url where the request will be sent</param>
        /// <param name="name">Name of this request. For logging purposes only.</param>
        /// <param name="authenticatedRequest">Set to true, if the request requires authentication</param>
        /// <param name="resultCallback">Callback to be executed when a response is received</param>
        private IEnumerator HandleSimpleRequest(string method, string url, string name, bool authenticatedRequest, Action<string, bool> resultCallback)
        {
            Func<string, UnityWebRequest> requestCall;

            switch (method)
            {
                case UnityWebRequest.kHttpVerbGET:
                    requestCall = UnityWebRequest.Get;
                    break;
                case UnityWebRequest.kHttpVerbDELETE:
                    requestCall = UnityWebRequest.Delete;
                    break;
                default:
                    Debug.LogError($"Unknown request method {method}");
                    yield break;
            }
            
            using (UnityWebRequest webRequest = requestCall(url))
            {
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (authenticatedRequest)
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
                }
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogWarning($"{name} request error: {webRequest.error}");
                    resultCallback(webRequest.downloadHandler.text, false);
                    yield break;
                }

                string result = webRequest.downloadHandler?.text ?? "";
                resultCallback(result, true);
            }
        }
        
        /// <summary>
        /// Handles requests that contain a data payload
        /// </summary>
        /// <param name="method">A value from UnityWebRequest.kHttpVerb<...></param>
        /// <param name="url">The endpoint url where the request will be sent</param>
        /// <param name="payload">The data to be sent with the request. Must be a valid Json.</param>
        /// <param name="name">Name of this request. For logging purposes only.</param>
        /// <param name="authenticatedRequest">Set to true, if the request requires authentication</param>
        /// <param name="resultCallback">Callback to be executed when a response is received</param>
        private IEnumerator HandlePayloadRequest(string method, string url, string payload, string name, bool authenticatedRequest, Action<string, bool> resultCallback, string authorization = null)
        {
            Func<string, string, UnityWebRequest> requestCall;

            switch (method)
            {
                case UnityWebRequest.kHttpVerbPOST:
                    requestCall = UnityWebRequest.Post;
                    break;
                case UnityWebRequest.kHttpVerbPUT:
                    requestCall = UnityWebRequest.Put;
                    break;
                default:
                    Debug.LogError($"Unknown request method {method}");
                    yield break;
            }
            
            using (UnityWebRequest webRequest = requestCall(url, method))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                if (authenticatedRequest)
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
                }
                
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogWarning($"{name} request error: {webRequest.error}");
                    resultCallback(webRequest.downloadHandler.text, false);
                    yield break;
                }

                string result = "";
                if (webRequest.downloadHandler != null)
                {
                    result = webRequest.downloadHandler.text;
                }
                resultCallback(result, true);
            }
        }

        /// <summary>
        /// Validates that client has authenticated and has a token. Should be called before every non-anonymous endpoint call.
        /// </summary>
        /// <param name="requestName">For logging purposes only. Specifies which request was done when error occured.</param>
        /// <returns></returns>
        private void ValidateToken(string requestName)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError($"Attempting to make {requestName} requests before having received a token!");
            }
        }
    }
}