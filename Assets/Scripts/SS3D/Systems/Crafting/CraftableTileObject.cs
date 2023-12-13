using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Logging;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftableTileObject : NetworkActor, ICraftable
{
    public ItemId ItemId => GetItemId();

    private PlacedTileObject _tileObject;

    public void Consume()
    {
        _tileObject = GetComponent<PlacedTileObject>();
        Vector3 worldPosition = new Vector3(_tileObject.WorldOrigin.x, 0f, _tileObject.WorldOrigin.y);
        Subsystems.Get<TileSystem>().CurrentMap.ClearTileObject(worldPosition, _tileObject.Layer, _tileObject.Direction);
    }

    public void Craft(InteractionEvent interaction)
    {
        Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(_tileObject.tileObjectSO, interaction.Point, Direction.North, 
            false, false, false);
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
