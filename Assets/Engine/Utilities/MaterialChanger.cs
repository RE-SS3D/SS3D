using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialChanger
{
   public enum Palette01
    {
        black,
        blue,
        green,
        red

    }

    public static readonly Dictionary<Palette01, Color> Palette01ColorDictionary = new Dictionary<Palette01, Color>
    {
        { Palette01.black, new Color(0f,0f,0f) },
        { Palette01.blue, new Color(0.8078431f  , 1.105882f, 1.443137f) },
        { Palette01.green, new Color(1.066667f, 1.403922f, 0.8392157f) },
        { Palette01.red, new Color(1.105882f, 0.3843137f, 0.3843137f) }

    };

    public static void ChangeToColor(Material material, Palette01 color)
    {
        Color newColor;
        Palette01ColorDictionary.TryGetValue(color, out newColor);

        material.SetColor("Emission", newColor);
    }

    public static void ChangeObjectEmissionColor(GameObject gameObject, Palette01 color)
    {
        Color newColor;
        Palette01ColorDictionary.TryGetValue(color, out newColor);

        Material material = gameObject.GetComponent<Renderer>().material;
        
        material.SetColor("_EmissionColor", newColor);
    }
}
