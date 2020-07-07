using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

public class TurfConstructionInteraction : ConstructionInteraction
{
    /// <summary>
    /// The turf to construct
    /// </summary>
    public Turf Turf;
    /// <summary>
    /// If the turf can be constructed if there already is a turf
    /// </summary>
    public bool ConstructIfTurf = false;
    /// <summary>
    /// If the turf can be constructed if there is a wall
    /// </summary>
    public bool ConstructIfWall = false;

    public override string GetName(InteractionEvent interactionEvent)
    {
        return "Construct turf";
    }

    public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        
    }

    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        var tileObject = interactionEvent.Target.GetComponent<TileObject>();
        TileDefinition tile = tileObject.Tile;
        Turf existingTurf = tile.turf;

        if (existingTurf && (!ConstructIfTurf || existingTurf.isWall && !ConstructIfWall))
        {
            return false;
        }
        
        return base.CanInteract(interactionEvent);
    }

    protected override void StartDelayed(InteractionEvent interactionEvent)
    {
        var tileObject = interactionEvent.Target.GetComponent<TileObject>();
        TileDefinition tile = tileObject.Tile;
        tile.turf = Turf;
        Object.FindObjectOfType<TileManager>().UpdateTile(tileObject.transform.position, tile);
    }
}
