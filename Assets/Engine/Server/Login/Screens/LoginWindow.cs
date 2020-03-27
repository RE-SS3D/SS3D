using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Engine.Server.Login.Networking;
using SS3D.Engine.Server.Login.Data;
using SS3D.Engine.Server.Helpers;

namespace SS3D.Engine.Server.Login.Screens
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
        [SerializeField] private LoginScreen loginScreen = null;
        [SerializeField] private RegisterScreen registerScreen = null;
        [SerializeField] private CharacterScreen characterScreen = null;
        [SerializeField] private GameObject characterCreateScreen = null;
        [SerializeField] private GameObject characterSelectScreen = null;
        //Prefab that contains the "mini scene" for the character preview render texture
        [SerializeField] private GameObject characterPreviewRendererPrefab = null;

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
            loginManager.LoginServerClient.GetCharacters(HandleGetCharactersResponse);
        }
    
        public void ShowCharacterCreateScreen()
        {
            if (characterSelectScreen.activeSelf)
            {
                characterSelectScreen.SetActive(false);
            }
            characterCreateScreen.SetActive(true);
            characterScreen.ClearErrors();
            characterPreview.SetActive(true);
        }
    
        public void ShowCharacterSelectScreen()
        {
            if (characterCreateScreen.activeSelf)
            {
                characterCreateScreen.SetActive(false);
            }
            characterSelectScreen.SetActive(true);
            characterScreen.ClearErrors();
            characterPreview.SetActive(true);
        }

        public void HandleQuitButton()
        {
            Application.Quit();
        }

        public void HandleRegisterButton()
        {
            CredentialRequest credentialRequest = registerScreen.GetRegisterCredentials();
            if (credentialRequest != null)
            {
                loginManager.LoginServerClient.Register(credentialRequest, HandleRegisterResponse);
            }
        }

        public void HandleLoginButton()
        {
            CredentialRequest credentialRequest = loginScreen.GetLoginCredentials();
            if (credentialRequest != null)
            {
                loginManager.LoginServerClient.Authenticate(credentialRequest, HandleAuthenticateResponse);
            }
        }

        public void HandleCharacterCreateButton()
        {
            ShowCharacterCreateScreen();
            characterScreen.ClearCharacterNameInput();
        }

        public void HandleCharacterSaveButton()
        {
            CharacterRequest characterRequest = characterScreen.GetCharacterCustomisationData();
            if (string.IsNullOrEmpty(characterRequest.name))
            {
                characterScreen.DisplayErrorMessage("Name cannot be empty!");
            }

            loginManager.LoginServerClient.SaveCharacter(characterRequest, HandleCharacterCreateResponse);
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
            loginManager.LoginServerClient.DeleteCharacter(id.ToString(), HandleCharacterDeleteResponse);
            characterScreen.DeleteCharacterLocally(id);
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
        
        private void HandleRegisterResponse(string response, bool success)
        {
            if (success)
            {
                ShowLoginScreen();
                loginScreen.DisplayErrorMessage("Registration successful!");
                return;
            }
            
            registerScreen.DisplayErrorMessage(MessageResponse.CreateFromJSON(response).message);
        }

        private void HandleAuthenticateResponse(string response, bool success)
        {
            if (success)
            {
                loginManager.StoreToken(AuthenticationResponse.CreateFromJSON(response).token);
                loginScreen.ClearErrors();
                ShowCharacterScreen();
                return;
            }
            
            loginScreen.DisplayErrorMessage(MessageResponse.CreateFromJSON(response).message);
        }

        private void HandleGetCharactersResponse(string response, bool success)
        {
            if (success)
            {
                List<CharacterResponse> characters = JsonArray.Deserialize<CharacterResponse>(response, "characters").ToList();
                characterScreen.LoadCharacters(characters);
                return;
            }

            if (string.IsNullOrEmpty(response))
            {
                characterScreen.DisplayErrorMessage("No characters found.");
                return;
            }
            
            characterScreen.DisplayErrorMessage(MessageResponse.CreateFromJSON(response).message);
        }

        private void HandleCharacterCreateResponse(string response, bool success)
        {
            if (success)
            {
                ShowCharacterScreen();
                characterScreen.DisplayErrorMessage("Character created!");
                return;
            }
            
            characterScreen.DisplayErrorMessage(MessageResponse.CreateFromJSON(response).message);
        }

        private void HandleCharacterDeleteResponse(string response, bool success)
        {
            if (success)
            {
                ShowCharacterScreen();
                characterScreen.DisplayErrorMessage("Character has been deleted");
                return;
            }
            
            characterScreen.DisplayErrorMessage(MessageResponse.CreateFromJSON(response).message);
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
        }

        private void OnDestroy()
        {
            //Making sure we destroy the spinning human in the sky
            Destroy(characterPreview);
        }
    }
}
