using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public class ClothingContainers : MonoBehaviour
    {
        public AttachedContainer Gloves => Containers["Gloves"];
        public AttachedContainer Ears => Containers["Ears"];
        public AttachedContainer Jumpsuit => Containers["Jumpsuit"];
        public AttachedContainer Exosuit => Containers["Exosuit"];
        public AttachedContainer Glasses => Containers["Glasses"];
        public AttachedContainer Mask => Containers["Mask"];
        public AttachedContainer Head => Containers["Head"];
        public AttachedContainer Shoes => Containers["Shoes"];
        public AttachedContainer Accessory => Containers["Accessory"];
        // TODO: Replace with actual clothing storage
        public AttachedContainer SuitStorage => Containers["Suit Storage"];

        [NonSerialized]
        public static readonly string[] ClothingSlotNames = {"Ears", "Jumpsuit", "Exosuit", "Glasses", "Mask", "Gloves", "Head", "Shoes", "Accessory", "Suit Storage"};

        [NonSerialized]
        public readonly Dictionary<string, AttachedContainer> Containers = new();
    }
}