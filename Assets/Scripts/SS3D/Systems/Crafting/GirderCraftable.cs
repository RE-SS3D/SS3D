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
        GetComponent<AdvancedAdjacencyConnector>().OnMeshUpdate += HandleMeshUpdate;
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


    private void HandleMeshUpdate(object sender, MeshDirectionInfo info)
    {
        switch (info.Shape)
        {
            case AdjacencyShape.O:
                mesh = o;
                break;
            case AdjacencyShape.U:
                mesh = u;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleConnection());
                break;
            case AdjacencyShape.I:
                mesh = i;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.HasConnection(Direction.South) ? Direction.South : Direction.West);
                break;
            case AdjacencyShape.LNone:
                mesh = lNone;
                rotation = TileHelper.AngleBetween(Direction.SouthWest, adjacencyMap.GetDirectionBetweenTwoConnections());
                break;
            case AdjacencyShape.LSingle:
                mesh = lSingle;
                rotation = TileHelper.AngleBetween(Direction.SouthWest, adjacencyMap.GetDirectionBetweenTwoConnections());
                break;
            case AdjacencyShape.TNone:
                mesh = tNone;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                break;
            case AdjacencyShape.TSingleLeft:
                mesh = tSingleLeft;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                break;
            case AdjacencyShape.TSingleRight:
                mesh = tSingleRight;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                break;
            case AdjacencyShape.TDouble:
                mesh = tDouble;
                rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                break;
            case AdjacencyShape.XNone:
                mesh = xNone;
                break;
            case AdjacencyShape.XSingle:
                mesh = xSingle;
                Direction connectingDiagonal = adjacencyMap.GetSingleConnection(false);
                rotation = connectingDiagonal == Direction.NorthEast ? 180f :
                    connectingDiagonal == Direction.SouthEast ? 270f :
                    connectingDiagonal == Direction.SouthWest ? 0f : 90f;
                break;
            case AdjacencyShape.XOpposite:
                mesh = xOpposite;
                rotation = adjacencyMap.HasConnection(Direction.SouthWest) ? 0f : 90f;
                break;
            case AdjacencyShape.XSide:
                mesh = xSide;
                rotation = TileHelper.AngleBetween(Direction.SouthEast, adjacencyMap.GetDirectionBetweenTwoConnections(false)) - 45f;
                break;
            case AdjacencyShape.XTriple:
                mesh = xTriple;
                Direction nonConnectingDiagonal = adjacencyMap.GetSingleNonConnection(false);
                rotation = nonConnectingDiagonal == Direction.NorthEast ? 90f :
                    nonConnectingDiagonal == Direction.SouthEast ? 180f :
                    nonConnectingDiagonal == Direction.SouthWest ? -90f : 0f;
                break;
            case AdjacencyShape.XQuad:
                mesh = xQuad;
                break;
            default:
                Debug.LogError($"Received unexpected shape from advanced shape resolver: {shape}");
                mesh = o;
                break;
        }
    }


}
