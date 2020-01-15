using System.Collections.Generic;
using System.Linq;
using Login.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for reading user inputs from the character screen.
    /// Also contains logic for dynamically populating the character list and returning the currently selected character.
    /// Should be attached to the character screen UI panel.
    /// </summary>
    public class CharacterScreen : MonoBehaviour, IScreenWithErrors
    {
        [SerializeField] private TMP_InputField characterNameInput;
        [SerializeField] private TextMeshProUGUI characterNameOutput;
        [SerializeField] private TextMeshProUGUI errorElement;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private CharacterToggleElement togglePrefab;

        private Dictionary<int, CharacterResponse> characterDictionary = new Dictionary<int, CharacterResponse>();

        public string EnteredCharacterName => characterNameInput.text;

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
                label.text = character.Name;
                toggle.group = toggleGroup;
                characterDictionary.Add(character.Id, character);
                LoginWindow loginWindow = transform.root.GetComponentInChildren<LoginWindow>();
                characterToggleElement.Register(character.Id, loginWindow);
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

            characterNameOutput.text = characterDictionary[id].Name;
        }

        public void ClearCharacterNameInput()
        {
            characterNameInput.text = "";
        }

        public void ClearCharacterList()
        {
            GetComponentsInChildren<Toggle>().ToList().ForEach(characterToggle => Destroy(characterToggle.gameObject));
            characterDictionary = new Dictionary<int, CharacterResponse>();
        }
    }
}
