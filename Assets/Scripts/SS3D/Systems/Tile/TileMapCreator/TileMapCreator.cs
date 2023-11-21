using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using DynamicPanels;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.UI;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static SS3D.Systems.Tile.TileMapCreator.BuildGhostManager;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// In-game editor for placing and deleting items/objects in a tilemap.
    /// </summary>
    public class TileMapCreator : NetworkSystem, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GameObject _menuRoot;
        [SerializeField]
        private GameObject _contentRoot;
        [SerializeField]
        private GameObject _slotPrefab;
        [SerializeField]
        private TMP_Dropdown _layerPlacementDropdown;
        [SerializeField]
        private TMP_InputField _inputField;

        private bool _mouseOverUI = false;
        public bool MouseOverUI => _mouseOverUI;

        private bool _enabled = false;




        private TileSystem _tileSystem;

        private List<GenericObjectSo> _objectDatabase;
        private Controls.TileCreatorActions _controls;
        private InputSystem _inputSystem;
        private PanelTab _tab;

        [SerializeField]
        private BuildGhostManager _ghostManager;

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            _tab = PanelUtils.GetAssociatedTab(GetComponent<RectTransform>());
            ShowUI(false);
            _inputSystem = Subsystems.Get<InputSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            _inputSystem.ToggleAction(_controls.ToggleMenu, true);
            _controls.ToggleMenu.performed += HandleToggleMenu;
        }
        
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            AdjustGridWidth();
        }

        #region Handlers
       
        
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
            _tileSystem = Subsystems.Get<TileSystem>();
            LoadObjectGrid(new[] { TileLayer.Plenum }, false);
        }
        
       
        #endregion



        #region UI
        private void ShowUI(bool show)
        {
            if (!show)
            {
                _tab.Detach();
                _ghostManager.DestroyGhosts();
            }
            _tab.Panel.gameObject.SetActive(show);
            _menuRoot.SetActive(show);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOverUI = true;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
            if (!_ghostManager.IsDragging)
            {
                _inputSystem.ToggleAction(_controls.Place, false);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            if (!_ghostManager.IsDragging)
            {
                _inputSystem.ToggleAction(_controls.Place, true);
            }
        }
        public void OnInputFieldSelect()
        {
            _inputSystem.ToggleAllActions(false);
        }

        public void OnInputFieldDeselect()
        {
            _inputSystem.ToggleAllActions(true);
        }
        public void OnInputFieldChanged()
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
                Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorTab>().Setup(asset);
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
                Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorTab>().Setup(asset);
            }
        }
        /// <summary>
        /// Change the currently displayed tiles/items when a new layer is selected in the drop down menu.
        /// </summary>
        public void OnDropDownChange()
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
        /// Change number of columns in asset grid to fit it's width. Elements of the group will take as much width as possible, but won't exceed width of the menu.
        /// </summary>
        private void AdjustGridWidth()
        {
            GridLayoutGroup grid = _contentRoot.GetComponent<GridLayoutGroup>();
            float cellWidth = grid.cellSize.x;
            float paddingWidth = grid.spacing.x;
            float width = _menuRoot.GetComponent<RectTransform>().rect.width;
            int constraintCount = Convert.ToInt32(Math.Floor(width / (cellWidth + paddingWidth)));
            if (constraintCount != grid.constraintCount)
                grid.constraintCount = constraintCount;
        }
        #endregion
        
        #region Map
        private void ClearGrid()
        {
            for (int i = 0; i < _contentRoot.transform.childCount; i++)
            {
                _contentRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }
        [Server]
        public void LoadMap()
        {
            if (IsServer)
            {
                _tileSystem.Load();
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }
        }

        [Server]
        public void SaveMap()
        {
            if (IsServer)
            {
                _tileSystem.Save();
            }
            else
            {
                Log.Information(this, "Cannot save the map on a client");
            }
        }
        #endregion

        
    }
}