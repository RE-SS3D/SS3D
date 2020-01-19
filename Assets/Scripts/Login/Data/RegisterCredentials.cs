namespace Login.Data
{
    /// <summary>
    /// Data class to store user input from Registration screen
    /// </summary>
    public class RegisterCredentials
    {
        public RegisterCredentials(string email, string password, string password2)
        {
            Email = email;
            Password = password;
            Password2 = password2;
        }

        public string Email { get; }
        public string Password { get; }
        public string Password2 { get; }
    }
}