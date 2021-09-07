﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Basically a copy of HumanoidBOdyPartTargetSelector.cs
/// This manages intent and it's done to easily support other intents
/// </summary>
public class IntentManager : MonoBehaviour
{
    public IntentType selectedIntent;
    
    public Image intentImage;

    public Sprite spriteHelp;
    public Sprite spriteHarm;

    public Color colorHarm;
    public Color colorHelp;

    private Button IntentButton;

    public enum IntentType
    {
        Help,
        Harm
    }

    void Start()
    {
        IntentButton = GetComponent<Button>();
        IntentButton.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
		{
			SelectIntent();
		}

    // Select the body part and deselect others
    public void SelectIntent()
    {
        bool harm = selectedIntent == IntentType.Harm;
        selectedIntent = harm ? IntentType.Help : IntentType.Harm;
        intentImage.sprite = harm ? spriteHelp : spriteHarm;
        intentImage.color = harm ? colorHelp : colorHarm;
    }
}
