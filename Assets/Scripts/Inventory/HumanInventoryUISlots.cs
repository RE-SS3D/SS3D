using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class HumanInventoryUISlots
{
    [SerializeField]
    public Sprite defaultImage;

    [Header("Hands")]
    public Image itemLeftHand;

    public Image itemRightHand;

    public Image LeftHandActiveMarker;
    public Image RightHandActiveMarker;

    [Header("Body")]
    public Image bodyPanel;

    public Image itemHelmet;
    public Image itemGloves;
    public Image itemEars;
    public Image itemSuitStorage;
    public Image itemVest;
    public Image itemMask;
    public Image itemShoes;
    public Image itemGlasses;
    public Image itemShirt;

    [Header("Quick Access")]
    public Image itemCard;

    public Image itemBelt;
    public Image itemBackpack;
    public Image itemPocketLeft;
    public Image itemPocketRight;
}