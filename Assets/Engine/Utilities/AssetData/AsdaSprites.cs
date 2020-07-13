using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "SpritesData", menuName = "AssetData/Sprites", order = 1)]
public class AsdaSprites : ScriptableObject
{
    public Sprite Missing,
        Build, 
        Container,
        Discard,
        Door,
        Music,
        Power,
        Reinforce,
        Cancel,
        Take
        ;
}
