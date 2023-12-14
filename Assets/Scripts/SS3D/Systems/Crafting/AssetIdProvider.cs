using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Data.Enums;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using FishNet.Object;

namespace SS3D.Systems.Crafting
{
    public class AssetIdProvider : NetworkActor, IAssetRefProvider
    {
        public ItemId ItemId => GetItemId();

        public ItemId GetItemId()
        {
            string itemName = gameObject.name.Split('(')[0];

            if (!Enum.TryParse(itemName, out ItemId id))
            {
                Debug.LogError( $"id with name {itemName} not present in ItemId enums");
            }

            return id;
        }
    }
}
