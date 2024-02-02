using Coimbra;
using DynamicPanels;
using QuikGraph;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static SS3D.Systems.Crafting.CraftingRecipe;
using InputSystem = SS3D.Systems.Inputs.InputSystem;
using NetworkView = SS3D.Core.Behaviours.NetworkView;

public class CraftingMenu : NetworkView, IPointerEnterHandler, IPointerExitHandler
{

    private Controls.TileCreatorActions _controls;

    private InputSystem _inputSystem;

    /// <summary>
    ///  The model for a single slot, to display tile objects in the menu.
    /// </summary>
    [SerializeField]
    private GameObject _craftingSlotPrefab;

    /// <summary>
    /// Game object parent of the area in the tile map menu where the tile object slots will display.
    /// </summary>
    [SerializeField]
    private GameObject _contentRoot;

    protected override void OnStart()
    {
        base.OnStart();
        ShowUI(false);
        _inputSystem = Subsystems.Get<InputSystem>();
        _controls = _inputSystem.Inputs.TileCreator;
    }

    /// <summary>
    /// Called when pointer enter the UI of the menu.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
    }

    /// <summary>
    /// Called when pointer exit the UI of the menu.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
    }

    private void ShowUI(bool isShow)
    {        
        gameObject.SetActive(isShow);
    }

    /// <summary>
    /// Method called when the control to open the tilemap menu is performed.
    /// </summary>
    public void DisplayMenu(List<CraftingInteraction> interactions, InteractionEvent interactionEvent, InteractionReference reference)
    {
        ClearGrid();

        foreach (CraftingInteraction interaction in interactions)
        {
            Instantiate(_craftingSlotPrefab, _contentRoot.transform, true).GetComponent<CraftingAssetSlot>().Setup(interaction, interactionEvent, reference);
        }

        ShowUI(true);
    }

    public void HideMenu()
    {
        ShowUI(false);
    }


    /// <summary>
    /// Clear all tile slots in the content area of the tilemap menu.
    /// </summary>
    private void ClearGrid()
    {
        for (int i = 0; i < _contentRoot.transform.childCount; i++)
        {
            _contentRoot.transform.GetChild(i).gameObject.Dispose(true);
        }
    }
}
