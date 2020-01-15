using Login.Data;
using TMPro;
using UnityEngine;

namespace Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the registration screen.
    /// Should be attached to the registration screen UI panel.
    /// </summary>
    public class RegisterScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField passwordInput2;
        [SerializeField] private TextMeshProUGUI errorElement;

        public RegisterCredentials GetRegisterCredentials()
        {
            return new RegisterCredentials(emailInput.text, passwordInput.text, passwordInput2.text);
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
