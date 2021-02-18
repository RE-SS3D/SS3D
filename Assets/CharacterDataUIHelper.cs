using SS3D.Engine.Database;
using TMPro;
using UnityEngine;

public class CharacterDataUIHelper : MonoBehaviour
{
    public CharacterDatabaseObject character;
    
    public void SetCharacterName(TMP_InputField inputField)
    {
        string name = inputField.text;
        character.name = name;
    }

    public void SetCharacterNameUI()
    {
        // TODO: Use character.GetCharacterData()
    }
}
