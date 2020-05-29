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

    public RadialInteractionMenuUI menu;

    // Sets menu's selected petal and the interaction name
    public override void OnPointerEnter(PointerEventData eventData)
    {
        menu.selectedPetal = transform.GetComponent<RectTransform>();
        menu.interactionName.text = interaction;
        menu.objectName.text = objectName;
        base.OnPointerEnter(eventData);
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        menu.selectedPetal = null;
        menu.interactionName.text = null;
        menu.objectName.text = null;
        base.OnPointerExit(eventData);
    }
}
