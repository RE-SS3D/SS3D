using System;
using System.IO;
using System.Net;
using Login.Data;
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
            return ProcessWebRequest(() =>
                WebRequestMaker.Post(connectionAddress + Post_Authenticate, loginCredentials.ToJson()));
        }

        public ApiResponse GetCharacters(string authorizationToken)
        {
            return ProcessWebRequest(() =>
                WebRequestMaker.Get(connectionAddress + Get_CharacterList, authorizationToken));
        }

        public ApiResponse SaveCharacter(string authorizationToken, string name)
        {
            var json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("name", name);
            string payload = json.Print();
            return ProcessWebRequest(() => WebRequestMaker.Post(connectionAddress + Post_CreateCharacter, payload,
                WebRequestMaker.PostMethod.POST, authorizationToken));
        }

        public ApiResponse DeleteCharacter(string authorizationToken, int id)
        {
            return ProcessWebRequest(() => WebRequestMaker.Post(connectionAddress + Delete_DeleteCharacter + id, "",
                WebRequestMaker.PostMethod.DELETE, authorizationToken));
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
    }
}