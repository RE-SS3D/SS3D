using SS3D.Engine.Database;
using SS3D.Engine.Database.Objects;
using TMPro;
using UnityEngine;

/// <summary>
/// <b>
/// Updates the UI to the CharacterData loaded from the user
/// </b>
///
/// <para>
///     Manages the CharacterData UI, in the character customization
/// screen, updates the fields and saves the CharacterData back to the server
/// </para> 
/// </summary>
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
