using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// This helps out GeneralSettingsManager to set up graphics settings and sound via UI,
/// currently being used on the lobby menu
/// </summary>
public class GeneralSettingsUIHelper : MonoBehaviour
{
    // we reference the settings singleton
    private GeneralSettingsManager settings;
    
    // is SSRT enabled? (the weird thing seteron worked on)
    public Toggle toggleSSRT;

    // these are the graphic options buttons
    public Button[] graphicButtons;

    // these are the settings tabs buttons
    public Button[] tabsButtons;
    public GameObject[] tabs;
    // we need to know which tab we are in
    public int selectedTab = 0;

    // slider to control the master volume
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

        // assigns the sound value, once we have a way to save 
        // a user's config, we'll have a proper use for this
        soundSlider.value = AudioListener.volume;
        
        // subscribe to the event, so we update the buttons when that event is invoked
        GeneralSettingsManager.OnGraphicsChanged += UpdateButtonsState;
    }

    // make sure buttons update correctly
    // when we click a button, it is disabled and
    // the others will be interactable again
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
    // same thing as the buttons
    public void UpdateTabsState(int tabIndex) 
    {
    for (int i = 0; i < tabsButtons.Length; i++)
            {
                if (i != tabIndex)
                {
                    tabsButtons[i].interactable = true;
                    tabs[i].SetActive(false);
                }
                else
                {
                    tabsButtons[i].interactable = false;
                    tabs[i].SetActive(true);
                }
            }
    }

    // updates master volume
    public void UpdateMasterVolume()
    {
        // for now we only update the master volume
        // I don't care enough on doing specific sound sliders
        // the only exception would be bass boost slider lol
        settings.SetMasterVolume(soundSlider.value);
    }
    
    // prevent null ref errors
    private void OnDestroy()
    {
        GeneralSettingsManager.OnGraphicsChanged -= UpdateButtonsState;
    }
}
