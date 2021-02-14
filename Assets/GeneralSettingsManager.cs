using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GeneralSettingsManager : MonoBehaviour
{
    public static GeneralSettingsManager singleton { get; private set; }
    public static event System.Action<int> OnGraphicsChanged; 
    
    public SSRT ssrt;
    public GraphicSettings graphicSettings = GraphicSettings.low;
    
    public enum GraphicSettings
    {
        potato = 0,
        low = 1,
        medium = 2,
        high = 3
    }
    
    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            singleton = this;
        }
    }

    public void SetGraphicSettings(GraphicSettings level)
    {
        QualitySettings.SetQualityLevel((int)level ,true);
    }
    public void SetGraphicSettings(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
    }
}
