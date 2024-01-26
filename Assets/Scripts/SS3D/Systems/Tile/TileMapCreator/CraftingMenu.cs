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

public class CraftingMenu : NetworkSystem, IPointerEnterHandler, IPointerExitHandler
{

    private Controls.TileCreatorActions _controls;

    private InputSystem _inputSystem;

    private bool _mouseOverUI = false;

    private PanelTab _tab;

    /// <summary>
    /// List of tile objects and items to load in the tilemap menu, that will show in the slots.
    /// </summary>
    private List<GenericObjectSo> _objectDatabase;

    /// <summary>
    ///  The model for a single slot, to display tile objects in the menu.
    /// </summary>
    [SerializeField]
    private GameObject _craftingSlotPrefab;

    /// <summary>
    /// Is the menu enabled
    /// </summary>
    private bool _enabled = false;

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
        _inputSystem.ToggleAction(_controls.ToggleMenu, true);
        _objectDatabase = Subsystems.Get<TileSystem>().Loader.Assets;
    }
    
    public void DisplayMenu(List<TaggedEdge<RecipeStep, RecipeStepLink>> links, CraftingInteraction interaction, InteractionEvent interactionEvent)
    {
        LoadObjectGrid(links, interaction, interactionEvent);
    }

    /// <summary>
    /// Called when pointer enter the UI of the menu.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _mouseOverUI = true;
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
    }

    /// <summary>
    /// Called when pointer exit the UI of the menu.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseOverUI = false;
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
    }

    private void ShowUI(bool isShow)
    {        
        _tab.Panel.gameObject.SetActive(isShow);
    }

    /// <summary>
    /// Method called when the control to open the tilemap menu is performed.
    /// </summary>
    private void HandleToggleMenu(InputAction.CallbackContext context)
    {
        if (_enabled)
        {
            _inputSystem.ToggleActionMap(_controls, false, new[] { _controls.ToggleMenu });
            _inputSystem.ToggleCollisions(_controls, true);
        }
        else
        {
            _inputSystem.ToggleActionMap(_controls, true, new[] { _controls.ToggleMenu });
            _inputSystem.ToggleCollisions(_controls, false);
        }
        _enabled = !_enabled;
        ShowUI(_enabled);
    }


    /// <summary>
    /// Change number of columns in asset grid to fit it's width.
    /// Elements of the group will take as much width as possible, but won't exceed width of the menu.
    /// </summary>
    private void AdjustGridWidth()
    {
        GridLayoutGroup grid = _contentRoot.GetComponent<GridLayoutGroup>();
        float cellWidth = grid.cellSize.x;
        float paddingWidth = grid.spacing.x;
        float width = gameObject.GetComponent<RectTransform>().rect.width;
        int constraintCount = Convert.ToInt32(Math.Floor(width / (cellWidth + paddingWidth)));
        if (constraintCount != grid.constraintCount)
            grid.constraintCount = constraintCount;
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

    /// <summary>
    /// Load a list of tile objects and place them in the UI box grid.
    /// </summary>
    private void LoadObjectGrid(List<TaggedEdge<RecipeStep, RecipeStepLink>> links, CraftingInteraction interaction, InteractionEvent interactionEvent)
    {
        ClearGrid();
        
        foreach (TaggedEdge<RecipeStep, RecipeStepLink> link in links)
        {
            string recipeStepName = link.Target.Name;

            Instantiate(_craftingSlotPrefab, _contentRoot.transform, true).GetComponent<CraftingAssetSlot>().Setup(link, interaction, interactionEvent);
        }
    }
}
