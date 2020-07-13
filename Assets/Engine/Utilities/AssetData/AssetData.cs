using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetData : MonoBehaviour
{
    
    public static AsdaSprites Sprites;
    public static AsdaSounds Sounds;
//    public static AssetDataSounds Sounds;

    void Awake()
    {
        Sprites = Resources.Load<AsdaSprites>("SpritesData");
        Sounds = Resources.Load<AsdaSounds>("SoundsData");
    }
}
