using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Health;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the selected body part target on targets
/// </summary>
public class BodyPartTargetSelector : MonoBehaviour
{
    // Colors for the buttons
    public ColorBlock colors;
    // Selected part
    public BodyPartType SelectedBodyPartTypePart;
    // List of the buttons of the parts  
    public Button[] parts;

    private void Start()
    {
        // Assign the colors to the buttonss
        Setup();
    }

    // Select the body part and deselect others
    public void SelectBodyPart(Button selected)
    {
        selected.Select();
        // disables the selected button
        selected.interactable = false;
        SelectedBodyPartTypePart = SetBodyPartType(selected.name);
        foreach (Button part in parts)
        {
            if (part != selected)
            {
                // the enabled is in case the player is missing a limb
                if (part.enabled)
                {
                    part.OnDeselect(null);
                    part.interactable = true;
                }
            }
        }
    }

    // This handles the enum of the bodypart using a string
    // yes it's terrible
    private BodyPartType SetBodyPartType(string type)
    {
        return (BodyPartType) System.Enum.Parse(typeof(BodyPartType), type);
    }
    
    // Assign color to the buttons, and I wanted it to assign the event, however it doesn't work so I did manually
    // TODO: make onClick automatic
    [ContextMenu("Setup buttons")]
    public void Setup()
    {
        foreach (Button part in parts)
        {
            part.colors = colors;
            part.onClick.AddListener(delegate
            {
                SelectBodyPart(part);
            });
        }
    }
}
