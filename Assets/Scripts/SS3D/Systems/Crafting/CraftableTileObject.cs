using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Logging;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftableTileObject : NetworkActor, ICraftable
{
    public ItemId ItemId => GetItemId();

    [Server]
    public void Craft(InteractionEvent interaction)
    {
        var _tileObject = GetComponent<PlacedTileObject>();

        if (interaction.Source is not BuildInteraction buildInteraction) return;

        Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(_tileObject.tileObjectSO, TileHelper.GetClosestPosition(interaction.Point), Direction.North, 
            false, buildInteraction.Replace , false);
    }

    private ItemId GetItemId()
    {
        string itemName = gameObject.name.Split('(')[0];

        if (!Enum.TryParse(itemName, out ItemId id))
        {
            Log.Error(this, $"id with name {itemName} not present in ItemId enums");
        }

        return id;
    }
}
