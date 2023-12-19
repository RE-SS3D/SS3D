using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCraftable : MultiStepCraftable
{
    public override void Craft(IInteraction interaction, InteractionEvent interactionEvent)
    {
        switch (_currentStepNumber)
        {
            case 1: AddSheet();
                break;
            case 2: SpawnWall(interaction, interactionEvent);
                break;
        }

        _currentStepNumber++;
    }

    // Start is called before the first frame update
    void Start()
    {
        _currentStepNumber = 1;
        _stepAmount = 2;
    }

    private void AddSheet()
    {

    }

    private void SpawnWall(IInteraction interaction, InteractionEvent interactionEvent)
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
