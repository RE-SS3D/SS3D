using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Make sure all the "clothes" type container are at the end of this enum (starting from Shoes..).
    /// This is necessary for differentiating between clothes and non clothes containers in some methods. 
    /// </summary>
    public enum ContainerType
    {
        None,
        Pocket,
        Hand,
        Bag,
        Identification,
        Shoes,
        Gloves,
        Glasses,
        Mask,
        Ears,
        Head,
        Jumpsuit,
        ExoSuit,
    }
}

