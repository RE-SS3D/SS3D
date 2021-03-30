using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class GeneralSettingsManager : MonoBehaviour
{
    public static GeneralSettingsManager singleton { get; private set; }
    
    // every time we update the graphics we tell the GeneralSettingsUIHelper which one was selected
    public static event System.Action<int> OnGraphicsChanged; 
    
    // custom stuff Seteron did
    public SSRT ssrt;
    
    public GraphicSettings graphicSettings = GraphicSettings.low;
    
    // keep these the same as the ones in the quality settings
    public enum GraphicSettings
    {
        potato = 0,
        low = 1,
        medium = 2,
        high = 3
    }
 
    // singleton
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

        OnGraphicsChanged?.Invoke((int)level);
    }
    public void SetGraphicSettings(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        
        OnGraphicsChanged?.Invoke(level);
    }
    
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }
}
