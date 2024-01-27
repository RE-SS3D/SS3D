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
    private MeshRenderer sheetMesh;

    [SerializeField]
    private AdvancedAdjacencyConnector _sheetConnector;

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
                AddGlassSheet();
                break;
        }
    }

    public override void Craft(IInteraction interaction, InteractionEvent interactionEvent) { }

    private void AddMetalSheet()
    {
        sheetMesh.material = _palette;
        sheetMesh.enabled = true;
    }

    private void AddGlassSheet()
    {
        sheetMesh.material = _glass;
        sheetMesh.enabled = true;
    }


}
