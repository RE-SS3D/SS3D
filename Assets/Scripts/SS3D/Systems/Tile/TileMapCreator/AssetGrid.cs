using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;

/// <summary>
/// Handle the UI and displaying everything related to the tilemap menu building part.
/// </summary>
public class AssetGrid : Actor
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
    /// List of tile objects and items to load in the tilemap menu, that will show in the slots.
    /// </summary>
    private List<GenericObjectSo> _objectDatabase;

    /// <summary>
    /// Script orchestrating the menu UI.
    /// </summary>
    [SerializeField]
    private TileMapMenuSubSystem _menu;

    /// <summary>
    /// Game object parent of the area in the tile map menu where the tile object slots will display.
    /// </summary>
    [SerializeField]
    private GameObject _contentRoot;
    
    private TileSubSystem _tileSystem;

    public void Setup()
    {
        AddHandle(UpdateEvent.AddListener(HandleUpdate));
        _tileSystem = SubSystems.Get<TileSubSystem>();
        LoadCurrentCategory();
    }

    private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
    {
        AdjustGridWidth();
    }

    /// <summary>
    /// Show assets, that contain given string in their name.
    /// </summary>
    public void FindAssets(string text)
    {
        ClearGrid();
        foreach (GenericObjectSo asset in _objectDatabase)
        {
            if (!asset.name.Contains(text, StringComparison.OrdinalIgnoreCase)) continue;
            Instantiate(_slotPrefab, _contentRoot.transform).GetComponent<ConstructionSlot>().Setup(asset);
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
            Instantiate(_slotPrefab, _contentRoot.transform).GetComponent<ConstructionSlot>().Setup(asset);
        }
    }

    /// <summary>
    /// Change the currently displayed tiles/items when a new layer is selected in the drop down menu.
    /// </summary>
    private void LoadCurrentCategory()
    {
        int index = _layerPlacementDropdown.value;
        bool isItems = false;
        TileLayer[] layers = null;
        switch (index)
        {
            case 0:
                layers = new[] { TileLayer.Plenum };
                break;

            case 1:
                layers = new[] { TileLayer.Turf };
                break;

            case 2:
                layers = new[]
                {
                    TileLayer.FurnitureBase,
                    TileLayer.FurnitureTop
                };
                break;

            case 3:
                layers = new[]
                {
                    TileLayer.WallMountLow,
                    TileLayer.WallMountHigh
                };
                break;

            case 4:
                layers = new[]
                {
                    TileLayer.Wire,
                    TileLayer.Disposal,
                    TileLayer.PipeLeft,
                    TileLayer.PipeRight,
                    TileLayer.PipeSurface,
                    TileLayer.PipeMiddle
                };
                break;

            case 5:
                layers = new[]
                {
                    TileLayer.Overlays
                };
                break;

            case 6:
                LoadObjectGrid(null, true);
                isItems = true;
                break;

            default:
                ClearGrid();
                break;
        }

        if ((layers != null) || isItems)
        {
            LoadObjectGrid(layers, isItems);
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
