using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Make sure all the "clothes" type container are at the end of this enum (starting from Shoes..).
    /// This is necessary for differentiating between clothes and non clothes containers in some methods. 
    /// </summary>
    [SerializeField]
    public enum ContainerType
    {
        None = 0,
        Pocket = 1 << 0,
        Hand = 1 << 1,
        Shoes = 1 << 2,
        Bag = 1 << 3,
        Identification = 1 << 4,
        Gloves = 1 << 5,
        Glasses = 1 << 6,
        Mask = 1 << 7,
        Ears = 1 << 8,
        Head = 1 << 9,
        Jumpsuit = 1 << 10,
        ExoSuit = 1 << 11,
    }
}

