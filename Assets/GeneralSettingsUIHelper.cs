using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GeneralSettingsUIHelper : MonoBehaviour
{
    public Toggle toggleSSRT;

    public Button[] graphicButtons;

    public Button[] tabs;
    
    private void Start()
    {
        GeneralSettingsManager settings = GeneralSettingsManager.singleton;

        toggleSSRT.isOn = settings.ssrt.enabled;

        tabs[0].Select();
        
        graphicButtons[(int)settings.graphicSettings].Select();
    }
}
