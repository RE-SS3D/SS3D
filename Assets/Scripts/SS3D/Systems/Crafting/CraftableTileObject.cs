using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftableTileObject : NetworkActor, ICraftable
{

    [Server]
    public void Craft(IInteraction interaction, InteractionEvent interactionEvent)
    {
        var _tileObject = GetComponent<PlacedTileObject>();

        bool replace = false;
        Direction direction = Direction.North;

        if (interaction is BuildInteraction buildInteraction)
        {
            replace = buildInteraction.Replace;

        }

        Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(_tileObject.tileObjectSO, 
            TileHelper.GetClosestPosition(interactionEvent.Target.GetGameObject().transform.position), direction, false, replace, false);
    }
}
