using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Tile.TileRework;
using UnityEngine;

public class ItemConstructionInteraction : ConstructionInteraction
{
    public TileObjectSo ObjectToConstruct;
    public Direction ObjectDirection;

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

        if (TileManager.Instance.CanBuild(ObjectToConstruct, placedObject.transform.position, ObjectDirection))
        {
            return true;
        }

        return base.CanInteract(interactionEvent);
    }

    protected override void StartDelayed(InteractionEvent interactionEvent)
    {
        var tileObject = interactionEvent.Target.GetComponent<PlacedTileObject>();
        TileManager.Instance.SetTileObject(ObjectToConstruct, tileObject.transform.position, ObjectDirection);
    }
}
