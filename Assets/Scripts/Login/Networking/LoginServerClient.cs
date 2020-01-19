using System;
using System.IO;
using System.Net;
using Login.Data;
using UnityEngine;
using Utilities;

namespace Login
{
    /// <summary>
    /// Class responsible for communicating with the Login API.
    /// Contains all supported endpoints and any special logic required to serialize/deserialize requests/responses
    /// </summary>
    public class LoginServerClient
    {
        public static readonly string prefix = "/api/";

        //Method_EndpointName
        //Anonymous requests
        private static readonly string Get_Heartbeat = prefix + "heartbeat";
        private static readonly string Post_Register = prefix + "user/register";
        private static readonly string Post_Authenticate = prefix + "user/authenticate";

        //Authenticated requests
        private static readonly string Get_CharacterList = prefix + "character/all";
        private static readonly string Post_CreateCharacter = prefix + "character/create";
        private static readonly string Delete_DeleteCharacter = prefix + "character/";

        private string connectionAddress;
        private string token;

        public LoginServerClient(string connectionAddress)
        {
            this.connectionAddress = connectionAddress;
        }

        public bool IsLoginServerAlive()
        {
            ApiResponse apiResponse = ProcessWebRequest(() =>
                WebRequestMaker.Get(connectionAddress + Get_Heartbeat));
            return apiResponse.IsSuccess();
        }

        public ApiResponse Register(RegisterCredentials registerCredentials)
        {
            string payload = LoginCredentials.From(registerCredentials).ToJson();
            return ProcessWebRequest(() =>
                WebRequestMaker.Post(connectionAddress + Post_Register, payload));
        }

        public ApiResponse Authenticate(LoginCredentials loginCredentials)
        {
            ApiResponse response = ProcessWebRequest(() =>
                WebRequestMaker.Post(connectionAddress + Post_Authenticate, loginCredentials.ToJson()));

            if (response.IsSuccess())
            {
                token = response.GetToken();
            }

            return response;
        }

        public ApiResponse GetCharacters()
        {
            if (!IsTokenValid("Get Characters"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }
            
            return ProcessWebRequest(() =>
                WebRequestMaker.Get(connectionAddress + Get_CharacterList, token));
        }

        public ApiResponse SaveCharacter(string name)
        {
            if (!IsTokenValid("Save Character"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }
            
            var json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("name", name);
            string payload = json.Print();
            return ProcessWebRequest(() => WebRequestMaker.Post(connectionAddress + Post_CreateCharacter, payload,
                WebRequestMaker.PostMethod.POST, token));
        }

        public ApiResponse DeleteCharacter(int id)
        {
            if (!IsTokenValid("Delete Character"))
            {
                return new ApiResponse(ApiResponse.ResponseType.Error, "");
            }
            
            return ProcessWebRequest(() => WebRequestMaker.Post(connectionAddress + Delete_DeleteCharacter + id, "",
                WebRequestMaker.PostMethod.DELETE, token));
        }

        private ApiResponse ProcessWebRequest(Func<string> request)
        {
            try
            {
                string response = request.Invoke();
                return new ApiResponse(ApiResponse.ResponseType.Success, response);
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    return new ApiResponse(ApiResponse.ResponseType.Error, "");
                }

                string response = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                return new ApiResponse(ApiResponse.ResponseType.Error, response);
            }
        }
        
        /// <summary>
        /// Validates that client has authenticated and has a token. Should be called before every non-anonymous endpoint call.
        /// </summary>
        /// <param name="requestName">For logging purposes only. Specifies which request was done when error occured.</param>
        /// <returns></returns>
        private bool IsTokenValid(string requestName)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError($"Attempting to make {requestName} requests before having received a token!");
                return false;
            }

            return true;
        }
    }
}