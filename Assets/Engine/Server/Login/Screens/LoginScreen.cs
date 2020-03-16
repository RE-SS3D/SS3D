using TMPro;
using UnityEngine;
using SS3D.Engine.Server.Login.Data;

namespace SS3D.Engine.Server.Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the login screen.
    /// Should be attached to the login screen UI panel.
    /// </summary>
    public class LoginScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField emailInput = null;
        [SerializeField] private TMP_InputField passwordInput = null;
        [SerializeField] private TextMeshProUGUI errorElement = null;

        public CredentialRequest GetLoginCredentials()
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
            if (string.IsNullOrEmpty(email))
            {
                DisplayErrorMessage("Email cannot be empty!");
                return false;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                DisplayErrorMessage("Password cannot be empty!");
                return false;
            }

            return true;
        }
    }
}
