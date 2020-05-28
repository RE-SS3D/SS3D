using SS3D.Engine.Interactions.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialMenuButton : Button
{
    public string interaction;

    public RadialInteractionMenuUI menu;
    public TextMeshProUGUI center;

    // Sets menu's selected petal and the interaction name
    public override void OnPointerEnter(PointerEventData eventData)
    {
        menu.selectedPetal = transform.GetComponent<RectTransform>();
        center.text = interaction;
        base.OnPointerEnter(eventData);
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        menu.selectedPetal = null;
        center.text = null;
        base.OnPointerExit(eventData);
    }
}
