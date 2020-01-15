using System.Collections.Generic;

namespace Login.Data
{
    /// <summary>
    /// Class responsible for handling the different kinds of responses that may be received from the API.
    /// Mainly to do with deserializing from Json strings into something we can use in C#.
    /// </summary>
    //TODO: if the API expands, it would be prudent to have a separate class for each type of response object. I did not do it now because it would be more overhead than usefulness.
    public class ApiResponse
    {
        public enum ResponseType
        {
            Success,
            Error
        }

        private ResponseType responseType;
        private string body;

        public ApiResponse(ResponseType responseType, string body)
        {
            this.responseType = responseType;
            this.body = body;
        }

        public bool IsSuccess()
        {
            return responseType == ResponseType.Success;
        }

        public string GetError()
        {
            return ParseStringField("message");
        }

        public string GetToken()
        {
            return ParseStringField("token");
        }

        public List<CharacterResponse> GetCharacters()
        {
            return ParseCharacterField();
        }

        /*
         *Expected structure:
         * {
         *     "<fieldName>":"Some text here"
         * }
         */
        private string ParseStringField(string fieldName)
        {
            var json = new JSONObject(body);
            string response = "";
            //Delegate will only execute if response has a <fieldName> member
            json.GetField(fieldName, delegate(JSONObject o) { response = o.str; });
            return response;
        }
        
        /*
         *Expected structure:
         * {
         *     "characters":
         *     {
         *        "id": 166
         *        "userId": 11
         *        "name": "Sparkly Mc Dorkinson"
         *     },
         *     {
         *        "id": 169
         *        "userId": 11
         *        "name": "Barnaby"
         *     }
         * }
         */
        private List<CharacterResponse> ParseCharacterField()
        {
            var json = new JSONObject(body);
            List<CharacterResponse> characterResponses = new List<CharacterResponse>();
            json.GetField("characters", delegate(JSONObject o)
            {
                o.list.ForEach(character =>
                {
                    int id = -1;
                    string name = "";
                    character.GetField("id", delegate(JSONObject idObject) { id = (int)idObject.i; });
                    character.GetField("name", delegate(JSONObject nameObject) { name = nameObject.str; });

                    if (id >= 0 && !string.IsNullOrEmpty(name))
                    {
                        CharacterResponse characterResponse = new CharacterResponse(id, name);
                        characterResponses.Add(characterResponse);
                    }
                });
            });
            return characterResponses;
        }
    }
}