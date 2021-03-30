using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GeneralSettingsUIHelper : MonoBehaviour
{
    private GeneralSettingsManager settings;
    
    public Toggle toggleSSRT;

    public Button[] graphicButtons;

    public Button[] tabs;

    public int selectedTab = 0;

    public Slider soundSlider;
    
    private void Start()
    {
        // TODO: make different post processing for each quality
        
        // set singleton
        settings = GeneralSettingsManager.singleton;

        // update buttons to the selected graphics (if we ever have the external settings option
        // and to load user settings
        UpdateButtonsState((int)settings.graphicSettings);
        
        // disable the SSRT, only for the lowest setting I think
        toggleSSRT.isOn = settings.ssrt.enabled;
        
        // set selected tab to the first one
        selectedTab = 0;
        UpdateTabsState(selectedTab);
        
        // subscribe to the event
        GeneralSettingsManager.OnGraphicsChanged += UpdateButtonsState;
    }

    // make sure buttons update correctly
    private void UpdateButtonsState(int quality)
    {
        for (int i = 0; i < graphicButtons.Length; i++)
        {
            if (i != quality)
            {
                graphicButtons[i].interactable = true;
            }
            else
            {
                graphicButtons[i].interactable = false;
            }
        }
    }
    
    // make sure tabs update correctly
    public void UpdateTabsState(int tabIndex) 
    {
    for (int i = 0; i < tabs.Length; i++)
            {
                if (i != tabIndex)
                {
                    tabs[i].interactable = true;
                }
                else
                {
                    tabs[i].interactable = false;
                }
            }
    }

    // updates master volume
    public void UpdateMasterVolume()
    {
        settings.SetMasterVolume(soundSlider.value);
    }
    // prevent null ref errors
    private void OnDestroy()
    {
        GeneralSettingsManager.OnGraphicsChanged -= UpdateButtonsState;
    }
}
