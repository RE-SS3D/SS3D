using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class HumanAttachmentPoints
{
    [Header("Hands")]
    public GameObject LeftHand;

    public GameObject RightHand;

    [Header("Body")]
    public GameObject Helmet;

    public GameObject Gloves;
    public GameObject Ears;
    public GameObject SuitStorage;
    public GameObject Vest;
    public GameObject Mask;
    public GameObject Shoes;
    public GameObject Glasses;
    public GameObject Shirt;

    [Header("Quick Access")]
    public GameObject Card;

    public GameObject Belt;
    public GameObject Backpack;
    public GameObject PocketLeft;
    public GameObject PocketRight;
}