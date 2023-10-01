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
        public bool LeftHand;
        public bool RightHand;
        public bool LeftPocket;
        public bool RightPocket;
        public bool LeftGlove;
        public bool RightGlove;
        public bool LeftShoe;
        public bool RightShoe;
        public bool Hat;
        public bool Glasses;
        public bool LeftEar;
        public bool RightEar;
        public bool Jumpsuit;

        public ItemId LeftHandItem;
        public ItemId RightHandItem;
        public ItemId LeftPocketItem;
        public ItemId RightPocketItem;
        public ItemId LeftGloveItem;
        public ItemId RightGloveItem;
        public ItemId LeftShoeItem;
        public ItemId RightShoeItem;
        public ItemId HatItem;
        public ItemId GlassesItem;
        public ItemId LeftEarItem;
        public ItemId RightEarItem;
        public ItemId JumpsuitItem;
    }
}
