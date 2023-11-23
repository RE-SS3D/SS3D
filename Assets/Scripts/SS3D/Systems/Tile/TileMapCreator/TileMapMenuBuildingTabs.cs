using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;

/// <summary>
/// Handle the UI and displaying everything related to the tilemap menu building part.
/// </summary>
public class TileMapMenuBuildingTabs : Actor
{
    /// <summary>
    ///  The model for a single slot, to display tile objects in the menu.
    /// </summary>
    [SerializeField]
    private GameObject _slotPrefab;


    /// <summary>
    /// Dropdown to select the layer to display in the menu.
    /// </summary>
    [SerializeField]
    private TMP_Dropdown _layerPlacementDropdown;

    /// <summary>
    /// Input field to search for specific tile objects or items in the menu.
    /// </summary>
    [SerializeField]
    private TMP_InputField _inputField;

    /// <summary>
    /// List of tile objects and items to load in the tilemap menu, that will show in the slots.
    /// </summary>
    private List<GenericObjectSo> _objectDatabase;

    /// <summary>
    /// Script orchestrating the menu UI.
    /// </summary>
    [SerializeField]
    private TileMapMenu _menu;

    /// <summary>
    /// Game object parent of the area in the tile map menu where the tile object slots will display.
    /// </summary>
    [SerializeField]
    private GameObject _contentRoot;

    private TileSystem _tileSystem;

    private InputSystem _inputSystem;


    public void Setup()
    {
        AddHandle(UpdateEvent.AddListener(HandleUpdate));
        _inputSystem = Subsystems.Get<InputSystem>();
        _tileSystem= Subsystems.Get<TileSystem>();

        LoadObjectGrid(new[] { TileLayer.Plenum }, false);
    }

    private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
    {
        AdjustGridWidth();
    }

    /// <summary>
    /// Called when the input field to search for tile objects is selected.
    /// </summary>
    private void OnInputFieldSelect()
    {
        _inputSystem.ToggleAllActions(false);
    }

    /// <summary>
    /// Called when the input field to search for tile objects is selected.
    /// </summary>
    private void OnInputFieldDeselect()
    {
        _inputSystem.ToggleAllActions(true);
    }

    /// <summary>
    /// Called when the text in the input field to search for tile objects is changed.
    /// </summary>
    private void OnInputFieldChanged()
    {
        ClearGrid();

        if (_inputField.text.Contains(' '))
        {
            // Replace spaces with underscores, since all asset names contain underscores
            _inputField.text = _inputField.text.Replace(' ', '_');
            // Prevent executing the same code twice
            return;
        }
        foreach (GenericObjectSo asset in _objectDatabase)
        {
            if (!asset.nameString.Contains(_inputField.text)) continue;
            Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorSlot>().Setup(asset);
        }
    }

    /// <summary>
    /// Load a list of tile objects and place them in the UI box grid.
    /// </summary>
    private void LoadObjectGrid(TileLayer[] allowedLayers, bool isItems)
    {
        ClearGrid();
        _objectDatabase = _tileSystem.Loader.Assets;
        foreach (GenericObjectSo asset in _objectDatabase)
        {
            switch (isItems)
            {
                case true when asset is not ItemObjectSo:
                case false when asset is ItemObjectSo:
                case false when asset is TileObjectSo so && !allowedLayers.Contains(so.layer):
                    continue;
            }
            Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorSlot>().Setup(asset);
        }
    }

    /// <summary>
    /// Change the currently displayed tiles/items when a new layer is selected in the drop down menu.
    /// </summary>
    private void OnDropDownChange()
    {
        int index = _layerPlacementDropdown.value;

        switch (index)
        {
            case 0:
                LoadObjectGrid(new[]
                {
                        TileLayer.Plenum
                    }, false);
                break;

            case 1:
                LoadObjectGrid(new[]
                {
                        TileLayer.Turf
                    }, false);
                break;

            case 2:
                LoadObjectGrid(new[]
                {
                        TileLayer.FurnitureBase,
                        TileLayer.FurnitureTop
                    }, false);
                break;

            case 3:
                LoadObjectGrid(new[]
                {
                        TileLayer.WallMountHigh,
                        TileLayer.WallMountLow
                    }, false);
                break;

            case 4:
                LoadObjectGrid(new[]
                {
                        TileLayer.Wire,
                        TileLayer.Disposal,
                        TileLayer.PipeLeft,
                        TileLayer.PipeRight,
                        TileLayer.PipeSurface,
                        TileLayer.PipeMiddle
                    }, false);
                break;

            case 5:
                LoadObjectGrid(new[] { TileLayer.Overlays }, false);
                break;

            case 6:
                LoadObjectGrid(null, true);
                break;

            default:
                ClearGrid();
                break;
        }
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
        float width = _menu.GetComponent<RectTransform>().rect.width;
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
}
