using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GirderCraftable : MultiStepCraftable
{

    [SerializeField]
    private MeshRenderer mesh;

    [SerializeField]
    private AdvancedAdjacencyConnector _sheetConnector;




    public override void Modify(IInteraction interaction, InteractionEvent interactionEvent)
    {
        AddSheet();
    }

    public override void Craft(IInteraction interaction, InteractionEvent interactionEvent)
    {
        SpawnGirder(interaction, interactionEvent);
    }

    // Start is called before the first frame update
    void Awake()
    {
        _stepAmount = 2;
    }

    private void AddSheet()
    {
        mesh.enabled = true;
    }

    private void SpawnGirder(IInteraction interaction, InteractionEvent interactionEvent)
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
