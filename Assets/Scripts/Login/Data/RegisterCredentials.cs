namespace Login.Data
{
    /// <summary>
    /// Data class to store user input from Registration screen
    /// </summary>
    public class RegisterCredentials
    {
        private string email;
        private string password;
        private string password2;

        public RegisterCredentials(string email, string password, string password2)
        {
            this.email = email;
            this.password = password;
            this.password2 = password2;
        }

        public string Email => email;
        public string Password => password;
        public string Password2 => password2;
    }
}