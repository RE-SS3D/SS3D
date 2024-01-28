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

        switch (step)
        {
            case "SteelGirderWithMetalSheet":
                AddMetalSheet();
                break;
            case "SteelGirderWithGlassSheet":
                AddWindowSheet();
                break;
            case "ReinforcedSteelGirder":
                AddStruts();
                break;
            case "ReinforcedSteelGirderWithBoltedMetalSheets":
                AddReinforcedSteelSheets();
                break;
            case "ReinforcedSteelGirderWithBoltedGlassSheets":
                AddReinforcedWindowSheets();
                break;
        }
    }

    public override void Craft(IInteraction interaction, InteractionEvent interactionEvent) { }

    private void AddMetalSheet()
    {
        _wallSheetMesh.material = _palette;
        _wallSheetMesh.enabled = true;
    }

    private void AddWindowSheet()
    {
        _windowSheetMesh.material = _glass;
        _windowSheetMesh.enabled = true;
    }

    private void AddReinforcedSteelSheets()
    {
        _lowerReinforcedSheetMesh.material = _palette;
        _lowerReinforcedSheetMesh.enabled = true;
        _higherReinforcedWallSheetMesh.material = _palette;
        _higherReinforcedWallSheetMesh.enabled = true;
    }

    private void AddReinforcedWindowSheets()
    {
        _lowerReinforcedSheetMesh.material = _palette;
        _lowerReinforcedSheetMesh.enabled = true;
        _higherReinforcedWindowSheetMesh.material = _palette;
        _higherReinforcedWindowSheetMesh.enabled = true;
    }

    private void AddStruts()
    {
        _struts.material = _palette;
        _struts.enabled = true;
    }


}
