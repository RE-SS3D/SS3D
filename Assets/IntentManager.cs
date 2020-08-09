using System.Collections;
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

    public enum IntentType
    {
        Help,
        Harm
    }

    public Button[] intents;
    
    private void Start()
    {
        // Assign the colors to the buttonss
        Setup();
    }

    // Select the body part and deselect others
    public void SelectIntent(Button selected)
    {
        selected.Select();
        // disables the selected button
        selected.interactable = false;
        selectedIntent = SetIntent(selected.name);
        foreach (Button intent in intents)
        {
            if (intent != selected)
            {
                if (intent.enabled)
                {
                    intent.OnDeselect(null);
                    intent.interactable = true;
                }
            }
        }
    }

    private IntentType SetIntent(string type)
    {
        return (IntentType) System.Enum.Parse(typeof(IntentType), type);
    }
    
    // Assign color to the buttons, and I wanted it to assign the event, however it doesn't work so I did manually
    // TODO: make onClick automatic
    [ContextMenu("Setup buttons")]
    public void Setup()
    {
        foreach (Button intent in intents)
        {
            intent.onClick.AddListener(delegate
            {
                SelectIntent(intent);
            });
        }
    }
}
