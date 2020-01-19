namespace Login.Data
{
    /// <summary>
    /// Data class to store user input from Login screen
    /// </summary>
    public class LoginCredentials
    {
        public LoginCredentials(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public static LoginCredentials From(RegisterCredentials registerCredentials)
        {
            return new LoginCredentials(registerCredentials.Email, registerCredentials.Password);
        }

        public string Email { get; }
        public string Password { get; }
        
        public string ToJson()
        {
            var json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("email", Email);
            json.AddField("password", Password);
            return json.Print();
        }
    }
}