﻿using Coimbra;
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


        private bool _enabled = false;
        private bool _itemPlacement = false;
        private bool _mouseOverUI = false;

        private Vector3 _lastSnappedPosition;
        private GenericObjectSo _selectedObject;

        private TileSystem _tileSystem;


        private List<GenericObjectSo> _objectDatabase;
        private Controls.TileCreatorActions _controls;
        private InputSystem _inputSystem;
        private PanelTab _tab;

        private Vector3 _dragStartPostion;
        private bool _isDragging;
        private bool _isDeleting;
        private Direction _direction = Direction.North;

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
            _controls.Place.started += HandlePlaceStarted;
            _controls.Place.performed += HandlePlacePerformed;
            _controls.Replace.performed += HandleReplace;
            _controls.Replace.canceled += HandleReplace;
            _controls.Rotate.performed += HandleRotate;
        }
        
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            ActivateGhosts();
            AdjustGridWidth();
            
            Vector3 position = TileHelper.GetPointedPosition(!_itemPlacement);
            // Move buildGhost, that sticks to the mouse. Currently it exists only if player is not dragging.
            if (_ghostManager._ghosts.Count == 1)
            {
                _ghostManager._ghosts.First().position = position;
                if (position != _lastSnappedPosition)
                {
                    RefreshGhost(_ghostManager._ghosts.First());
                }
            }

            if (_isDragging && (position != _lastSnappedPosition) && (_selectedObject != null))
            {
                // Delete all ghosts and instantiate new on correct positions. Currently it causes large fps drops.
                _ghostManager.DestroyGhosts();
                if (_controls.SquareDrag.phase == InputActionPhase.Performed)
                {
                    SquareDrag(position);
                }
                else
                {
                    LineDrag(position);
                }
            }
            _lastSnappedPosition = position;
        }

        #region Handlers
        public void HandleDeleteButton()
        {
            _isDeleting = true;
        }

        public void HandleBuildButton()
        {
            _isDeleting = false;
        }

        private void HandleRotate(InputAction.CallbackContext context)
        {
            _ghostManager.SetNextRotation();
        }

        private void HandlePlaceStarted(InputAction.CallbackContext context)
        {
            // Dragging is disabled for items
            if (_itemPlacement)
                return;
            
            _isDragging = true;
            _dragStartPostion = TileHelper.GetPointedPosition(true);
        }

        private void HandlePlacePerformed(InputAction.CallbackContext context)
        {
            _isDragging = false;
            
            if (_mouseOverUI)
            {
                _inputSystem.ToggleAction(_controls.Place, false);
            }
            
            if (!_isDeleting)
            {
                PlaceOnGhosts();
            }
            else
            {
                DeleteOnGhosts();
            }

            _ghostManager.DestroyGhosts();
            
            if (_selectedObject == null) return;
            _ghostManager.CreateGhost(_selectedObject.prefab, TileHelper.GetPointedPosition(!_itemPlacement), _direction);
        }

        private void HandleReplace(InputAction.CallbackContext context)
        {
            foreach (BuildGhost buildGhost in _ghostManager._ghosts)
            {
                RefreshGhost(buildGhost);
            }
        }
        
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
        
        /// <summary>
        /// Place all objects form buildGhost
        /// </summary>
        private void PlaceOnGhosts()
        {
            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;

            foreach (BuildGhost buildGhost in _ghostManager._ghosts)
            {
                _tileSystem.RpcPlaceObject(_selectedObject.nameString, buildGhost.position, _ghostManager.Dir, isReplacing);
            }
        }

        /// <summary>
        /// Delete all objects, that are under ghosts
        /// </summary>
        private void DeleteOnGhosts()
        {
            if (_itemPlacement)
            {
                FindAndDeleteItem();
            }
            else
            {
                foreach (BuildGhost buildGhost in _ghostManager._ghosts)
                {
                    _tileSystem.RpcClearTileObject(_selectedObject.nameString, buildGhost.position, _ghostManager.Dir);
                }
            }
        }

        /// <summary>
        /// Update material of buildGhost based build (or anything else) mode and ghosts position  
        /// </summary>
        public void RefreshGhost(BuildGhost buildGhost)
        {
            if (_isDeleting)
            {
                buildGhost.ChangeGhostColor(BuildMatMode.Delete);
            }
            else if (_itemPlacement)
            {
                buildGhost.ChangeGhostColor(BuildMatMode.Valid);
            }
            else
            {
                bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
                RpcSendCanBuild(_selectedObject.nameString, buildGhost.position, _ghostManager.Dir, isReplacing, LocalConnection);
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void RpcSendCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting, NetworkConnection conn)
        {

            var tileSystem = Subsystems.Get<TileSystem>();

            TileObjectSo tileObjectSo = (TileObjectSo)tileSystem.GetAsset(tileObjectSoName);

            if (tileObjectSo == null)
            {
                Log.Error(this, "Asset is not found");
                return;
            }

            bool canBuild = tileSystem.CanBuild(tileObjectSo, placePosition, dir, replaceExisting);
            RpcReceiveCanBuild(conn, placePosition, canBuild);
        }

        [TargetRpc]
        private void RpcReceiveCanBuild(NetworkConnection conn, Vector3 placePosition, bool canBuild)
        {
            // Find correct buildGhost to update its material
            for (int i = 0; i < _ghostManager._ghosts.Count; i++)
            {
                BuildGhost buildGhost = _ghostManager._ghosts[i];
                if (buildGhost.position != placePosition) continue;

                if (canBuild)
                {
                    buildGhost.ChangeGhostColor(BuildMatMode.Valid);
                }
                else
                {
                    buildGhost.ChangeGhostColor(BuildMatMode.Invalid);
                }
                return;
            }
        }

        private void FindAndDeleteItem()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                PlacedItemObject placedItem = hitInfo.collider.gameObject.GetComponent<PlacedItemObject>();
                if (placedItem != null)
                {
                    _tileSystem.RpcClearItemObject(placedItem.NameString, placedItem.gameObject.transform.position);
                }
            }
        }
        #endregion

        #region Drag algorithms
        private void LineDrag(Vector3 position)
        {
            Vector2 firstPoint = new(_dragStartPostion.x, _dragStartPostion.z);
            Vector2 secondPoint = new(position.x, position.z);
            Vector3[] tiles = MathUtility.FindTilesOnLine(firstPoint, secondPoint)
                .Select(x => new Vector3(x.x, position.y, x.y)).ToArray();
            
            foreach (Vector3 tile in tiles)
            {
                var ghost = _ghostManager.CreateGhost(_selectedObject.prefab, tile, _direction);
                RefreshGhost(ghost);
            }
        }

        private void SquareDrag(Vector3 position)
        {
            int x1 = (int)Math.Min(_dragStartPostion.x, position.x);
            int x2 = (int)Math.Max(_dragStartPostion.x, position.x);
            int y1 = (int)Math.Min(_dragStartPostion.z, position.z);
            int y2 = (int)Math.Max(_dragStartPostion.z, position.z);

            for (int i = y1; i <= y2; i++)
            {
                for (int j = x1; j <= x2; j++)
                {
                    Vector3 tile = new(j, 0, i);
                    BuildGhost buildGhost = _ghostManager.CreateGhost(_selectedObject.prefab, tile, _direction);
                    RefreshGhost(buildGhost);
                }
            }
        }
        
        #endregion
        
        #region Ghosts 
       

    
        
        /// <summary>
        /// Activate all buildGhosts. This method is important, because for some reason network objects disable themselves after a few frames.
        /// </summary>
        private void ActivateGhosts()
        {
            foreach (BuildGhost buildGhost in _ghostManager._ghosts)
            {
                if (!buildGhost.ghost.activeSelf)
                {
                    buildGhost.ghost.SetActive(true);
                }
            }
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
            if (!_isDragging)
            {
                _inputSystem.ToggleAction(_controls.Place, false);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            if (!_isDragging)
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
        public void SetSelectedObject(GenericObjectSo genericObjectSo)
        {
            _itemPlacement = genericObjectSo switch
            {
                TileObjectSo => false,
                ItemObjectSo => true,
                _ => _itemPlacement,
            };
            _selectedObject = genericObjectSo;

            _ghostManager.DestroyGhosts();
            _ghostManager.CreateGhost(genericObjectSo.prefab, TileHelper.GetPointedPosition(!_itemPlacement), _direction);
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