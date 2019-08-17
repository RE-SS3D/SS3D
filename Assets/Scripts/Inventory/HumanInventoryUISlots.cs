using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class HumanInventoryUISlots
{
    [Header("Hands")]
    public ItemSlot slotLeftHand;

    public ItemSlot slotRightHand;

    public Image LeftHandActiveMarker;
    public Image RightHandActiveMarker;

    [Header("Body")]
    public Image bodyPanel;

    public ItemSlot slotHelmet;
    public ItemSlot slotGloves;
    public ItemSlot slotEars;
    public ItemSlot slotSuitStorage;
    public ItemSlot slotVest;
    public ItemSlot slotMask;
    public ItemSlot slotShoes;
    public ItemSlot slotGlasses;
    public ItemSlot slotShirt;

    [Header("Quick Access")]
    public ItemSlot slotCard;

    public ItemSlot slotBelt;
    public ItemSlot slotBackpack;
    public ItemSlot slotPocketLeft;
    public ItemSlot slotPocketRight;

    [Header("Reference Helper")]
    public List<ItemSlot> SlotsList;
}