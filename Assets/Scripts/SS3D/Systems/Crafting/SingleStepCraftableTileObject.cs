using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using UnityEngine;

/// <summary>
/// A simple script for tile objects that can be crafted in a single step.
/// </summary>
public class SingleStepCraftableTileObject : SingleStepCraftable
{

    [Server]
    public override void Craft(IInteraction interaction, InteractionEvent interactionEvent)
    {
        var _tileObject = GetComponent<PlacedTileObject>();

        bool replace = false;
        Direction direction = Direction.North;

        if (interaction is CraftingInteraction craftingInteraction)
        {
            replace = craftingInteraction.Replace;
        }

        Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(_tileObject.tileObjectSO, 
            TileHelper.GetClosestPosition(interactionEvent.Target.GetGameObject().transform.position), direction, false, replace, false);
    }

    /// <summary>
    /// In single step crafting, current object should not be modified, instead, a new object should be spawned.
    /// </summary>
    public override void Modify(IInteraction interaction, InteractionEvent interactionEvent){ }
}
