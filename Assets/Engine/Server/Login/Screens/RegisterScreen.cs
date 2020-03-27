using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using SS3D.Engine.Server.Login.Data;

namespace SS3D.Engine.Server.Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the registration screen.
    /// Should be attached to the registration screen UI panel.
    /// </summary>
    public class RegisterScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField emailInput = null;
        [SerializeField] private TMP_InputField passwordInput = null;
        [SerializeField] private TMP_InputField passwordInput2 = null;
        [SerializeField] private TextMeshProUGUI errorElement = null;

        public CredentialRequest GetRegisterCredentials()
        {
            if (IsInputValid())
            {
                CredentialRequest credentialRequest = new CredentialRequest();
                credentialRequest.email = emailInput.text;
                credentialRequest.password = passwordInput.text;
                return credentialRequest;
            }

            return null;
        }
        
        public void DisplayErrorMessage(string error)
        {
            errorElement.text = error;
            errorElement.gameObject.SetActive(true);
        }

        public void ClearErrors()
        {
            errorElement.text = "";
            errorElement.gameObject.SetActive(false);
        }

        private bool IsInputValid()
        {
            string email = emailInput.text;
            string password = passwordInput.text;
            string password2 = passwordInput2.text;
            if (!Regex.IsMatch(email, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
            {
                DisplayErrorMessage("That is not a valid email address.");
                return false;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                DisplayErrorMessage("Password cannot be empty!");
                return false;
            }

            if (Regex.IsMatch(password, "[<>'\"`]"))
            {
                DisplayErrorMessage("Password may not contain < > \" ' ` characters.");
                return false;
            }

            if (password.Length < 6)
            {
                DisplayErrorMessage("Password must be at least 6 symbols long.");
                return false;
            }

            if (password != password2)
            {
                DisplayErrorMessage("Passwords must match.");
                return false;
            }

            return true;
        }
    }
}
