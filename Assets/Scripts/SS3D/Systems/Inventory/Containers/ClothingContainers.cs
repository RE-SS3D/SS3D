using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public class ClothingContainers : MonoBehaviour
    {
        public ContainerDescriptor Gloves => Containers["Gloves"];
        public ContainerDescriptor Ears => Containers["Ears"];
        public ContainerDescriptor Jumpsuit => Containers["Jumpsuit"];
        public ContainerDescriptor Exosuit => Containers["Exosuit"];
        public ContainerDescriptor Glasses => Containers["Glasses"];
        public ContainerDescriptor Mask => Containers["Mask"];
        public ContainerDescriptor Head => Containers["Head"];
        public ContainerDescriptor Shoes => Containers["Shoes"];
        public ContainerDescriptor Accessory => Containers["Accessory"];
        // TODO: Replace with actual clothing storage
        public ContainerDescriptor SuitStorage => Containers["Suit Storage"];

        [NonSerialized]
        public static readonly string[] ClothingSlotNames = {"Ears", "Jumpsuit", "Exosuit", "Glasses", "Mask", "Gloves", "Head", "Shoes", "Accessory", "Suit Storage"};

        [NonSerialized]
        public readonly Dictionary<string, ContainerDescriptor> Containers = new();
    }
}