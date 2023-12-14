using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeIngredient : NetworkActor, IRecipeIngredient
{
    public ItemId ItemId => GetItemId();

    [Server]
    public void Consume()
    {
        NetworkObject.Despawn();
    }

    private ItemId GetItemId()
    {
        string itemName = gameObject.name.Split('(')[0];

        if (!Enum.TryParse(itemName, out ItemId id))
        {
            Debug.LogError($"id with name {itemName} not present in ItemId enums");
        }

        return id;
    }
}
