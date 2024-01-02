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
    [SerializeField, Tooltip("A mesh where no edges are connected")]
    private Mesh o;

    [SerializeField, Tooltip("A mesh where South connects to same type")]
    private Mesh u;

    [SerializeField, Tooltip("A mesh where South and south edges are connected")]
    private Mesh i;

    [SerializeField, Tooltip("A mesh where the South and West edges are connected, no corners")]
    private Mesh lNone;
    [SerializeField, Tooltip("A mesh where the South and West edges are connected, and NE is a corner")]
    private Mesh lSingle;

    [SerializeField, Tooltip("A mesh where the South, West, and East edges are connected, no corners")]
    private Mesh tNone;
    [SerializeField, Tooltip("A mesh where the South, West, and East edges are connected, NW is a corner")]
    private Mesh tSingleRight;
    [SerializeField, Tooltip("A mesh where the South, West, and East edges are connected, NE is a corner")]
    private Mesh tSingleLeft;
    [SerializeField, Tooltip("A mesh where South, West, and East edges are connected, NW & NE are corners")]
    private Mesh tDouble;

    [SerializeField, Tooltip("A mesh where all edges are connected, no corners")]
    private Mesh xNone;
    [SerializeField, Tooltip("A mesh where all edges are connected, SW is a corner")]
    private Mesh xSingle;
    [SerializeField, Tooltip("A mesh where all edges are connected, SW & SW are corners")]
    private Mesh xSide;
    [SerializeField, Tooltip("A mesh where all edges are connected, NW & SE are corners")]
    private Mesh xOpposite;
    [SerializeField, Tooltip("A mesh where all edges are connected, all but NE are corners")]
    private Mesh xTriple;
    [SerializeField, Tooltip("A mesh where all edges are connected, all corners")]
    private Mesh xQuad;

    [SerializeField]
    private MeshFilter mesh;

    private bool _updateSheetMesh = false;


    public override void Modify(IInteraction interaction, InteractionEvent interactionEvent)
    {
        AddSheet();
        _currentStepNumber++;
    }

    public override void Craft(GameObject instance, IInteraction interaction, InteractionEvent interactionEvent)
    {
        SpawnGirder(interaction, interactionEvent);
    }

    // Start is called before the first frame update
    void Awake()
    {
        _currentStepNumber = 0;
        _stepAmount = 2;
        GetComponent<AdvancedAdjacencyConnector>().OnMeshUpdate += HandleMeshUpdate;
    }

    private void AddSheet()
    {
        _updateSheetMesh = true;
        mesh.gameObject.SetActive(true);
        mesh.mesh = o;
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


    private void HandleMeshUpdate(object sender, MeshDirectionInfo info)
    {
        if (!_updateSheetMesh) return;

        switch (info.Shape)
        {
            case AdjacencyShape.O:
                mesh.mesh = o;
                break;
            case AdjacencyShape.U:
                mesh.mesh = u;
                break;
            case AdjacencyShape.I:
                mesh.mesh = i;
                break;
            case AdjacencyShape.LNone:
                mesh.mesh = lNone;
                break;
            case AdjacencyShape.LSingle:
                mesh.mesh = lSingle;
                break;
            case AdjacencyShape.TNone:
                mesh.mesh = tNone;
                break;
            case AdjacencyShape.TSingleLeft:
                mesh.mesh = tSingleLeft;
                break;
            case AdjacencyShape.TSingleRight:
                mesh.mesh = tSingleRight;
                break;
            case AdjacencyShape.TDouble:
                mesh.mesh = tDouble;
                break;
            case AdjacencyShape.XNone:
                mesh.mesh = xNone;
                break;
            case AdjacencyShape.XSingle:
                mesh.mesh = xSingle;
                break;
            case AdjacencyShape.XOpposite:
                mesh.mesh = xOpposite;
                break;
            case AdjacencyShape.XSide:
                mesh.mesh = xSide;
                break;
            case AdjacencyShape.XTriple:
                mesh.mesh = xTriple;
                break;
            case AdjacencyShape.XQuad:
                mesh.mesh = xQuad;
                break;
            default:
                mesh.mesh = o;
                break;
        }

        Quaternion localRotation = mesh.transform.localRotation;
        Vector3 eulerRotation = localRotation.eulerAngles;
        localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);

        mesh.transform.localRotation = localRotation;
    }
}
