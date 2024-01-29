using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
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
using static UnityEngine.GraphicsBuffer;

public class GirderCraftable : MultiStepCraftable
{

    [SerializeField]
    private MeshRenderer _wallSheetMesh;

    [SerializeField]
    private MeshRenderer _windowSheetMesh;

    [SerializeField]
    private MeshRenderer _lowerReinforcedSheetMesh;

    [SerializeField]
    private MeshRenderer _higherReinforcedWallSheetMesh;

    [SerializeField]
    private MeshRenderer _higherReinforcedWindowSheetMesh;

    [SerializeField]
    private MeshRenderer _supportMesh;

    [SerializeField]
    private AdvancedAdjacencyConnector _sheetConnector;

    [SerializeField]
    private MeshRenderer _struts;

    [SerializeField]
    private Material _palette;

    [SerializeField]
    private Material _glass;


    public override void Modify(IInteraction interaction, InteractionEvent interactionEvent, string step)
    {
        _currentStepName = step;

        ClearAllMeshes();


        switch (step)
        {
            case "SteelGirderWithMetalSheet":
                AddMetalSheet();
                break;
            case "SteelGirderWithGlassSheet":
                AddWindowSheet();
                break;
            case "ReinforcedSteelGirderWithStruts":
                AddStruts();
                AddSupports();
                break;
            case "ReinforcedSteelGirderWithBoltedReinforcedMetalSheets":
                AddStruts();
                AddSupports();
                AddReinforcedSteelSheets();
                break;
            case "ReinforcedSteelGirderWithBoltedGlassSheets":
                AddStruts();
                AddSupports();
                AddReinforcedWindowSheets();
                break;
            case "ReinforcedSteelGirderWithBoltedMetalSheets":
                AddStruts();
                AddSupports();
                AddMetalSheet();
                break;
            case "ReinforcedSteelGirder":
                AddSupports();
                break;
        }
    }

    public override GameObject Craft(IInteraction interaction, InteractionEvent interactionEvent) { return gameObject; }

    private void AddMetalSheet()
    {
        _wallSheetMesh.enabled = true;
    }

    private void AddWindowSheet()
    {
        _windowSheetMesh.enabled = true;
    }

    private void AddReinforcedSteelSheets()
    {
        _higherReinforcedWallSheetMesh.enabled = true;
    }

    private void AddLowerReinforcedSteelSheets()
    {
        _lowerReinforcedSheetMesh.enabled = true;
    }

    private void AddReinforcedWindowSheets()
    {
        _higherReinforcedWindowSheetMesh.enabled = true;
    }

    private void AddStruts()
    {
        _struts.enabled = true;
    }

    private void AddSupports()
    {
        _supportMesh.enabled = true;
    }



    private void ClearAllMeshes()
    {
        _wallSheetMesh.enabled = false;
        _windowSheetMesh.enabled = false;
        _lowerReinforcedSheetMesh.enabled = false;
        _higherReinforcedWallSheetMesh.enabled = false;
        _higherReinforcedWindowSheetMesh.enabled = false;
        _struts.enabled = false;
        _supportMesh.enabled = false;
    }


}
