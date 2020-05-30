using SS3D.Engine.Interactions.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialMenuButton : Button
{
    public string objectName;
    public string interaction;
    [HideInInspector]
    public float angle;

    public RadialInteractionMenuUI menu;

    // Sets menu's selected petal and the interaction name
    void PetalSelect()
    {
        menu.selectedPetal = transform.GetComponent<RectTransform>();
        menu.interactionName.text = interaction;
        menu.objectName.text = objectName;
    }

    private void Update()
    {
        if (menu != null)
        {
            if (menu.mouseAngle >= angle - menu.buttonAngle
                && menu.mouseAngle < angle + menu.buttonAngle)
            {
                if (menu.mouseDistance > menu.buttonMaxDistance)
                {
                    PetalSelect();
                }
            }
        }
    }
}
