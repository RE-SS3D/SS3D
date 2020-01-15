using System.Text.RegularExpressions;
using Login.Data;
using Login.Screens;
using UnityEngine;

namespace Login
{
    /// <summary>
    /// MonoBehaviour in charge of handling the login UI.
    /// This includes logic for deciding which screen to show, what logic applies to each UI element
    /// what errors to display and what validation to do.
    /// Should be attached to LoginWindow UI prefab.
    /// </summary>
    public class LoginWindow : MonoBehaviour
    {
        //UI components that contain their respective screen panels
        [SerializeField] private LoginScreen loginScreen;
        [SerializeField] private RegisterScreen registerScreen;
        [SerializeField] private CharacterScreen characterScreen;
        [SerializeField] private GameObject characterCreateScreen;
        [SerializeField] private GameObject characterSelectScreen;
        //Prefab that contains the "mini scene" for the character preview render texture
        [SerializeField] private GameObject characterPreviewRendererPrefab;

        private GameObject characterPreview;
        private LoginManager loginManager;

        private void Start()
        {
            ShowLoginScreen();
            characterPreview = Instantiate(characterPreviewRendererPrefab);
            characterPreview.SetActive(false);
            loginManager = transform.parent.GetComponent<LoginManager>();
            if (loginManager == null)
            {
                Debug.LogError("Login Window cannot find Login Manager! All LoginWindow instances need to be children of a GameObject with a LoginManager component.");
            }
        }

        public void ShowLoginScreen()
        {
            HideAllScreens();
            loginScreen.gameObject.SetActive(true);
        }

        public void ShowRegisterScreen()
        {
            HideAllScreens();
            registerScreen.gameObject.SetActive(true);
        }
    
        public void ShowCharacterScreen()
        {
            HideAllScreens();
            characterScreen.gameObject.SetActive(true);
            ApiResponse characterResult = loginManager.CallCharacterListEndpoint();
            characterScreen.LoadCharacters(characterResult.GetCharacters());
        }
    
        public void ShowCharacterCreateScreen()
        {
            if (characterSelectScreen.activeSelf)
            {
                characterSelectScreen.SetActive(false);
            }
            characterCreateScreen.SetActive(true);
            characterPreview.SetActive(true);
        }
    
        public void ShowCharacterSelectScreen()
        {
            if (characterCreateScreen.activeSelf)
            {
                characterCreateScreen.SetActive(false);
            }
            characterSelectScreen.SetActive(true);
            characterPreview.SetActive(true);
        }

        public void HandleQuitButton()
        {
            Application.Quit();
        }

        public void HandleRegisterButton()
        {
            RegisterCredentials registerCredentials = registerScreen.GetRegisterCredentials();

            if (!Regex.IsMatch(registerCredentials.Email, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
            {
                registerScreen.DisplayErrorMessage("That is not a valid email address.");
                return;
            }
            
            if (string.IsNullOrEmpty(registerCredentials.Password))
            {
                registerScreen.DisplayErrorMessage("Password cannot be empty!");
                return;
            }

            if (Regex.IsMatch(registerCredentials.Password, "[<>'\"`]"))
            {
                registerScreen.DisplayErrorMessage("Password may not contain < > \" ' ` characters.");
                return;
            }

            if (registerCredentials.Password.Length < 6)
            {
                registerScreen.DisplayErrorMessage("Password must be at least 6 symbols long.");
                return;
            }

            if (registerCredentials.Password != registerCredentials.Password2)
            {
                registerScreen.DisplayErrorMessage("Passwords must match.");
                return;
            }

            ApiResponse result = loginManager.CallRegisterEndpoint(registerCredentials);

            if (!result.IsSuccess())
            {
                registerScreen.DisplayErrorMessage(result.GetError());
                return;
            }
            
            ShowLoginScreen();
            loginScreen.DisplayErrorMessage("Registration successful!");
        }

        public void HandleLoginButton()
        {
            LoginCredentials loginCredentials = loginScreen.GetLoginCredentials();
            
            if (string.IsNullOrEmpty(loginCredentials.Email))
            {
                loginScreen.DisplayErrorMessage("Email cannot be empty!");
                return;
            }
            
            if (string.IsNullOrEmpty(loginCredentials.Password))
            {
                loginScreen.DisplayErrorMessage("Password cannot be empty!");
                return;
            }
            
            ApiResponse result = loginManager.CallAuthenticateEndpoint(loginCredentials);
            
            if (!result.IsSuccess())
            {
                loginScreen.DisplayErrorMessage(result.GetError());
                return;
            }
            
            loginScreen.ClearErrors();
            ShowCharacterScreen();
        }

        public void HandleCharacterCreateButton()
        {
            ShowCharacterCreateScreen();
            characterScreen.ClearCharacterNameInput();
        }

        public void HandleCharacterSaveButton()
        {
            string enteredName = characterScreen.EnteredCharacterName;
            if (string.IsNullOrEmpty(enteredName))
            {
                characterScreen.DisplayErrorMessage("Name cannot be empty!");
            }

            ApiResponse createResult = loginManager.CallCharacterCreateEndpoint(enteredName);

            if (!createResult.IsSuccess())
            {
                characterScreen.DisplayErrorMessage(createResult.GetError());
                return;
            }
            
            ShowCharacterScreen();
            characterScreen.DisplayErrorMessage($"Character {enteredName} created!");
        }

        public void HandleCharacterToggle(int id, bool isOn)
        {
            if (!isOn)
            {
                ShowCharacterScreen();
                return;
            }
            
            ShowCharacterSelectScreen();
            characterScreen.SetCharacterPreviewName(id);
        }

        public void HandleCharacterDeleteButton(int id)
        {
            ApiResponse result = loginManager.CallCharacterDeleteEndpoint(id);
            if (!result.IsSuccess())
            {
                characterScreen.DisplayErrorMessage(result.GetError());
                return;
            }
            
            ShowCharacterScreen();
            characterScreen.DisplayErrorMessage("Character has been deleted");
        }

        public void HandleCharacterSelectButton()
        {
            CharacterResponse characterResponse = characterScreen.GetSelectedCharacterData();
            if (characterResponse == null)
            {
                characterScreen.DisplayErrorMessage("Could not find character data.");
                return;
            }

            loginManager.SpawnPlayer(characterResponse);
        }
    
        private void HideAllScreens()
        {
            loginScreen.gameObject.SetActive(false);
            registerScreen.gameObject.SetActive(false);
            characterScreen.gameObject.SetActive(false);
            characterCreateScreen.SetActive(false);
            characterSelectScreen.SetActive(false);
            loginScreen.ClearErrors();
            registerScreen.ClearErrors();
            characterScreen.ClearErrors();
            characterScreen.ClearCharacterList();
        }

        private void OnDestroy()
        {
            //Making sure we destroy the spinning human in the sky
            Destroy(characterPreview);
        }
    }
}
