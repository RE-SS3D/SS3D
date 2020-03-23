using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SS3D.Engine.Server.Login.Data;

namespace SS3D.Engine.Server.Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the character screen.
    /// Also contains logic for dynamically populating the character list and returning the currently selected character.
    /// Should be attached to the character screen UI panel.
    /// </summary>
    public class CharacterScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField characterNameInput = null;
        [SerializeField] private TextMeshProUGUI characterNameOutput = null;
        [SerializeField] private TextMeshProUGUI errorElement = null;
        [SerializeField] private ToggleGroup toggleGroup = null;
        [SerializeField] private CharacterToggleElement togglePrefab = null;

        private Dictionary<int, CharacterResponse> characterDictionary = new Dictionary<int, CharacterResponse>();


        public CharacterRequest GetCharacterCustomisationData()
        {
            CharacterRequest characterRequest = new CharacterRequest();
            characterRequest.name = characterNameInput.text;
            return characterRequest;
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

        public void LoadCharacters(List<CharacterResponse> characters)
        {
            ClearCharacterList();
            
            if (characters.Count == 0)
            {
                DisplayErrorMessage("No characters found.");
                return;
            }
            
            characters.ForEach(character =>
            {
                CharacterToggleElement characterToggleElement = Instantiate(togglePrefab, toggleGroup.transform);
                Toggle toggle = characterToggleElement.GetComponent<Toggle>();
                TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>();
                label.text = character.name;
                toggle.group = toggleGroup;
                characterDictionary.Add(Int32.Parse(character.id), character);
                LoginWindow loginWindow = transform.root.GetComponentInChildren<LoginWindow>();
                characterToggleElement.Register(Int32.Parse(character.id), loginWindow);
            });
        }

        public CharacterResponse GetSelectedCharacterData()
        {
            List<CharacterToggleElement> characterToggles =
                toggleGroup.GetComponentsInChildren<CharacterToggleElement>().ToList();
            CharacterToggleElement toggledElement = characterToggles.FirstOrDefault(characterToggle =>
            {
                Toggle toggle = characterToggle.GetComponent<Toggle>();
                return toggle.isOn;
            });
            if (toggledElement && characterDictionary.ContainsKey(toggledElement.Id))
            {
                return characterDictionary[toggledElement.Id];
            }

            return null;
        } 

        public void SetCharacterPreviewName(int id)
        {
            if (!characterDictionary.ContainsKey(id))
            {
                Debug.Log($"Could not find name of character with Id: {id}.");
                return;
            }

            characterNameOutput.text = characterDictionary[id].name;
        }

        public void ClearCharacterNameInput()
        {
            characterNameInput.text = "";
        }

        public void DeleteCharacterLocally(int id)
        {
            characterDictionary.Remove(id);
        }

        private void ClearCharacterList()
        {
            GetComponentsInChildren<Toggle>().ToList().ForEach(characterToggle => Destroy(characterToggle.gameObject));
            characterDictionary = new Dictionary<int, CharacterResponse>();
        }
    }
}
