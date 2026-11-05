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
        None = 0,
        Pocket = 1 << 0,
        Hand = 1 << 1,
        Bag = 1 << 2,
        Identification = 1 << 3,
        ShoeLeft = 1 << 4,
        ShoeRight = 1 << 5,
        Glasses = 1 << 6,
        Mask = 1 << 7,
        Head = 1 << 8,
        Jumpsuit = 1 << 9,
        ExoSuit = 1 << 10,
        GloveLeft = 1 << 11,
        GloveRight = 1 << 12,
        EarLeft = 1 << 13,
        EarRight = 1 << 14,
        Belt = 1 << 15,
        Pda = 1 << 16,
        SuitStorage = 1 << 17,
    }

    public static class ContainerTypeExtensions
    {
        /// <summary>
        /// True for clothes-type containers (worn on the body), false for hands/pockets/bags/ID/PDA.
        /// Relies on the enum ordering guaranteed by the doc comment on <see cref="ContainerType"/>.
        /// </summary>
        public static bool IsWornSlot(this ContainerType type)
        {
            return type >= ContainerType.ShoeLeft;
        }
    }
}

