using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialChanger
{
   public enum Palette01
    {
        black,
        blue,
        blueEmission,
        green,
        greenEmission,
        red,
        redEmission
    }

    public static readonly Dictionary<Palette01, Color> Palette01ColorDictionary = new Dictionary<Palette01, Color>
    {
        { Palette01.black, new Color(0f,0f,0f) },
        
        { Palette01.blue, new Color(0.4039216f  , 0.5529412f, 0.7215686f) },
        { Palette01.green, new Color(0.5333334f, 0.7019608f, 0.4196078f) },
        { Palette01.red, new Color(0.7254902f, 0.2862745f, 0.2862745f) },
        
        { Palette01.blueEmission, new Color(0.8078431f  , 1.105882f, 1.443137f) },
        { Palette01.greenEmission, new Color(1.066667f, 1.403922f, 0.8392157f) },
        { Palette01.redEmission, new Color(1.105882f, 0.3843137f, 0.3843137f) },
    };

    public static Color GetColor(Palette01 color)
    {
        Color newColor;
        Palette01ColorDictionary.TryGetValue(color, out newColor);

        return newColor;
    }
    
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
