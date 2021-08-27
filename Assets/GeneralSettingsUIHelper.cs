using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.UI;
using SS3D.Engine.Input;
using System.Reflection;
using UnityEngine.InputSystem;

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

    // prefab to use for generating key names
    public GameObject keyTitle;

    // prefab to use for generating group names
    public GameObject groupTitle;

    // prefab to use for generating spaces between group names and key names
    public GameObject blank;

    // prefab to use when generating the rebind buttons
    public GameObject bindingButton;

    // The "Content" object for the controls tab
    public GameObject controlPanel;

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

        Debug.Log(InputHelper.inp.Misc.LeftAlternateHeld.interactions);

        // add keybinding UI
        foreach (PropertyInfo field in InputHelper.inp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            // Filters out non-existing categories. These string names should be safe to hardcode
            if (field.PropertyType == typeof(InputControlScheme) || new List<string> { "asset", "bindingMask", "devices", "controlSchemes" }.Contains(field.Name))
                continue;
            string category = field.Name;
            category = category[0].ToString().ToUpper() + category.Substring(1);
            GameObject categoryGameObject = Instantiate(new GameObject(), controlPanel.transform);
            categoryGameObject.name = name;
            categoryGameObject.AddComponent<RectTransform>();
            categoryGameObject.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            categoryGameObject.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            categoryGameObject.AddComponent<HorizontalLayoutGroup>();
            categoryGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(502, categoryGameObject.GetComponent<RectTransform>().sizeDelta.y);
            categoryGameObject.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            categoryGameObject.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            GenerateStubs(categoryGameObject, category, "Titles");
            GenerateStubs(categoryGameObject, null, "Bindings");
            GenerateStubs(categoryGameObject, null, "AltBindings");

            foreach (PropertyInfo key in field.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                object inputGroup = InputHelper.inp.GetType().GetProperty(field.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(InputHelper.inp);
                object inputObj = inputGroup.GetType().GetProperty(key.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(inputGroup);
                if (inputObj.GetType().FullName != "UnityEngine.InputSystem.InputAction")
                    continue;
                InputAction inputAction = (InputAction) inputObj;
                if (!inputAction.interactions.Contains("Press") && !inputAction.interactions.Contains("Hold"))
                    continue;
                GameObject title = Instantiate(keyTitle, categoryGameObject.transform.Find("Titles"));
                //string name = Regex.Replace(key.Name, @"((?!$)[\p{Lu}])", " $1");
                title.transform.Find("Title").GetComponent<TMP_Text>().text = inputAction.name;
                GameObject binding = Instantiate(bindingButton, categoryGameObject.transform.Find("Bindings"));
                TMP_Text text = binding.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
                text.text = inputAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
                BindingButtonHelper buttonHelper = binding.AddComponent<BindingButtonHelper>();
                buttonHelper.inputActionToBind = inputAction;
                buttonHelper.text = text;
                Button button = binding.GetComponent<Button>();
                button.onClick.AddListener(() => buttonHelper.OnClicked());
                GameObject altBinding = Instantiate(bindingButton, categoryGameObject.transform.Find("AltBindings"));
                altBinding.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = "N/A";
            }

        }
    }

    // Some boilerplate that generates the category and the three objects used to store titles, binding and alt binding.
    private void GenerateStubs(GameObject parent, string name, string titleName)
    {
        GameObject titles = Instantiate(new GameObject(), parent.transform);
        titles.name = titleName;
        titles.AddComponent<RectTransform>();
        titles.transform.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        titles.transform.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        titles.AddComponent<VerticalLayoutGroup>();
        titles.transform.GetComponent<VerticalLayoutGroup>().childControlHeight = false;
        titles.transform.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
        GameObject title = Instantiate(groupTitle, titles.transform);
        title.transform.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        title.transform.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        title.transform.Find("Title").gameObject.GetComponent<TMP_Text>().text = name == null ? "" : name + " Controls";
        Instantiate(blank, titles.transform);
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
