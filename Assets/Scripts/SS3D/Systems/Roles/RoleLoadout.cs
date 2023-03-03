using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// The items that should spawn in the player inventory after embarking
    /// </summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "Roles/Loadout")]
    public class RoleLoadout : ScriptableObject
    {
        public bool leftHand;
        public bool rightHand;
        public bool leftPocket;
        public bool rightPocket;

        public ItemId leftHandItem;
        public ItemId rightHandItem;
        public ItemId leftPocketItem;
        public ItemId rightPocketItem;
    }
}
