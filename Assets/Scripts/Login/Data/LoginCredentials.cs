namespace Login.Data
{
    /// <summary>
    /// Data class to store user input from Login screen
    /// </summary>
    public class LoginCredentials
    {
        private string email;
        private string password;

        public LoginCredentials(string email, string password)
        {
            this.email = email;
            this.password = password;
        }

        public static LoginCredentials From(RegisterCredentials registerCredentials)
        {
            return new LoginCredentials(registerCredentials.Email, registerCredentials.Password);
        }

        public string Email => email;
        public string Password => password;
        
        public string ToJson()
        {
            var json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("email", Email);
            json.AddField("password", Password);
            return json.Print();
        }
    }
}