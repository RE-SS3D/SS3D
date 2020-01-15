using Login.Data;
using TMPro;
using UnityEngine;

namespace Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the login screen.
    /// Should be attached to the login screen UI panel.
    /// </summary>
    public class LoginScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI errorElement;

        public LoginCredentials GetLoginCredentials()
        {
            return new LoginCredentials(emailInput.text, passwordInput.text);
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
    }
}
