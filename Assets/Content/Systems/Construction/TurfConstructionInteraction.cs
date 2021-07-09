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
    public TileObjectSO ObjectToConstruct;
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
        return "Construct " + ObjectToConstruct.nameString;
    }

    public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        
    }

    public override bool CanInteract(InteractionEvent interactionEvent)
    {

        var placedObject = interactionEvent.Target.GetComponent<PlacedTileObject>();

        /*
        TileDefinition tile = placedObject.Tile;
        Turf existingTurf = tile.turf;
        */

        if (placedObject)
        {
            return false;
        }
        
        return base.CanInteract(interactionEvent);
    }

    protected override void StartDelayed(InteractionEvent interactionEvent)
    {
        var tileObject = interactionEvent.Target.GetComponent<PlacedTileObject>();

        /*
        TileDefinition tile = tileObject.Tile;
        tile.turf = ObjectToConstruct;
        Object.FindObjectOfType<TileManager>().UpdateTile(tileObject.transform.position, tile);
        */

        TileManager.Instance.SetTileObject(ObjectToConstruct, tileObject.transform.position, Direction.South);
    }
    
}
