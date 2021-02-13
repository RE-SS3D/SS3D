using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Database;
using TMPro;
using UnityEngine;

public class CharacterDataUIHelper : MonoBehaviour
{
    public CharacterData character;
    
    public void SetCharacterName(TMP_InputField inputField)
    {
        string name = inputField.text;
        character.name = name;
    }

    public void SetGender(TMP_Text text)
    {
        string gender = text.text;
        character.gender = gender;
    }
    
}
