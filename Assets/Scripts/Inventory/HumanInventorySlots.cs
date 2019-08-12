using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class HumanInventorySlots
{
    [Header("Hands")]
    [SerializeField]
    public Item LeftHand;

    public Transform LeftHandTransform;

    [SerializeField]
    public Item RightHand;
    public Transform RightHandTransform;

    [Header("Body")]
    [SerializeField]
    public Item Helmet;

    [SerializeField]
    public Item Gloves;

    [SerializeField]
    public Item Ears;

    [SerializeField]
    public Item SuitStorage;

    [SerializeField]
    public Item Vest;

    [SerializeField]
    public Item Mask;

    [SerializeField]
    public Item Shoes;
    
    [SerializeField]
    public Item Glasses;

    [SerializeField]
    public Item Shirt;

    [Header("Quick Access")]
    [SerializeField]
    public Item Card;

    [SerializeField]
    public Item Belt;

    [SerializeField]
    public Item Backpack;

    [SerializeField]
    public Item PocketLeft;

    [SerializeField]
    public Item PocketRight;
}