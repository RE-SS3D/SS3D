using Coimbra;
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
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// In-game editor for placing and deleting items/objects in a tilemap.
    /// </summary>
    public class TileMapCreator : NetworkSystem, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject _menuRoot;
        public GameObject _contentRoot;
        public GameObject _slotPrefab;
        public TMP_Dropdown _layerPlacementDropdown;
        [SerializeField]
        private TMP_InputField _inputField;

        private bool _enabled = false;
        private bool _itemPlacement = false;
        private bool _mouseOverUI = false;

        private Vector3 _lastSnappedPosition;
        private GenericObjectSo _selectedObject;

        private TileSystem _tileSystem;
        private List<GhostManager> _buildGhosts;
        private Plane _plane;

        private List<GenericObjectSo> _objectDatabase;
        private Controls.TileCreatorActions _controls;
        private InputSystem _inputSystem;
        private PanelTab _tab;

        private Vector3 _dragStartPostion;
        private bool _isDragging;
        private bool _isDeleting;

        [SerializeField]
        private Material _validConstruction;
        [SerializeField]
        private Material _invalidConstruction;
        [SerializeField]
        private Material _deleteConstruction;

        public void HandleDeleteButton()
        {
            _isDeleting = true;
        }

        public void HandleBuildButton()
        {
            _isDeleting = false;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _tab = PanelUtils.GetAssociatedTab(GetComponent<RectTransform>());
            _buildGhosts = new();
            ShowUI(false);
            _inputSystem = Subsystems.Get<InputSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            _inputSystem.ToggleAction(_controls.ToggleMenu, true);
            _controls.ToggleMenu.performed += HandleToggleMenu;
            _controls.Place.started += HandlePlaceStarted;
            _controls.Place.performed += HandlePlacePerformed;
            _controls.Replace.performed += HandleReplace;
            _controls.Replace.canceled += HandleReplace;
        }

        private void HandlePlaceStarted(InputAction.CallbackContext context)
        {
            _isDragging = true;
            _dragStartPostion = GetMousePosition(true);
        }

        private void HandlePlacePerformed(InputAction.CallbackContext context)
        {
            _isDragging = false;

            for (int i = 0; i < _buildGhosts.Count;)
            {
                _buildGhosts[i].gameObject.Dispose(true);
                _buildGhosts.RemoveAt(i);
            }
            Debug.Log("Stopped dragging");
        }

        private void HandleReplace(InputAction.CallbackContext context)
        {
            foreach (GhostManager buildGhost in _buildGhosts)
            {
                RefreshGhost(buildGhost);
            }
        }
        
        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (_enabled)
            {
                _inputSystem.ToggleActionMap(_controls, false, new[]
                {
                    _controls.ToggleMenu
                });
                _inputSystem.ToggleCollisions(_controls, true);
            }
            else
            {
                _inputSystem.ToggleActionMap(_controls, true, new[]
                {
                    _controls.ToggleMenu
                });
                _inputSystem.ToggleCollisions(_controls, false);
            }
            _enabled = !_enabled;
            ShowUI(_enabled);
            _tileSystem = Subsystems.Get<TileSystem>();
            _plane = new(Vector3.up, 0);

            LoadObjectGrid(new[] { TileLayer.Plenum }, false);
        }

        private void Update()
        {
            foreach (GhostManager buildGhost in _buildGhosts)
            {
                if (!buildGhost.gameObject.activeSelf)
                {
                    buildGhost.gameObject.SetActive(true);
                }
            }

            if (_buildGhosts.IsNullOrEmpty())
                return;

            Vector3 position = GetMousePosition(!_itemPlacement);
            GhostManager firstBuildGhost = _buildGhosts.First();
            firstBuildGhost.TargetPosition = position;
            if (_isDragging && (position != _lastSnappedPosition))
            {
                for (int i = 0; i < _buildGhosts.Count; )
                {
                    _buildGhosts[i].gameObject.Dispose(true);
                    _buildGhosts.RemoveAt(i);
                }
                foreach (Vector3 tile in FindTilesOnLine(_dragStartPostion, GetMousePosition(true)))
                {
                    GhostManager buildGhost = CreateGhost(GhostManager.BuildMatMode.Valid, _selectedObject.prefab, tile, Quaternion.identity);
                    buildGhost.TargetPosition = tile;
                    RefreshGhost(buildGhost);
                    _buildGhosts.Add(buildGhost);
                }
            }

            _lastSnappedPosition = position;
        }

        private void RefreshGhost(GhostManager buildGhost)
        {
            GhostManager.BuildMatMode mode;
            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
            if (_tileSystem.CanBuild((TileObjectSo)_selectedObject, buildGhost.gameObject.transform.position, buildGhost.Dir, isReplacing))
            {
                mode = GhostManager.BuildMatMode.Valid;
            }
            else
            {
                mode = GhostManager.BuildMatMode.Invalid;
            }
            buildGhost.ChangeGhostColor(mode);
            
        }
        public List<Vector3> FindTilesOnLine(Vector3 firstPoint, Vector3 secondPoint)
        {
            List<Vector3> list = new();
            int x = (int)firstPoint.x;
            int y = (int)firstPoint.z;
            int x2 = (int)secondPoint.x;
            int y2 = (int)secondPoint.z;
            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w<0) 
                dx1 = -1; 
            else if (w>0) 
                dx1 = 1;
            if (h<0) 
                dy1 = -1; 
            else if (h>0) 
                dy1 = 1;
            if (w<0) 
                dx2 = -1; 
            else if (w>0) 
                dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest>shortest)) 
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h<0) 
                    dy2 = -1; 
                else if (h>0) 
                    dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1 ;
            for (int i = 0; i <= longest; i++) 
            {
                list.Add(new(x, firstPoint.y, y));
                numerator += shortest;
                if (!(numerator < longest)) 
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                } 
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }
            return list;
        }
        private void ShowUI(bool show)
        {
            if (!show)
            {
                _tab.Detach();
            }
            _tab.Panel.gameObject.SetActive(show);
            _menuRoot.SetActive(show);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOverUI = true;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
            _inputSystem.ToggleActions(new []{_controls.Place, _controls.Delete}, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            _inputSystem.ToggleActions(new []{_controls.Place, _controls.Delete}, true);
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
        /// <summary>
        /// Loads a list of tile objects and places them in the UI box grid.
        /// </summary>
        /// <param name="allowedLayers"></param>
        /// <param name="isItems"></param>
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
                Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorTab>().Setup(asset);;
            }
        }
        /// <summary>
        /// Change the currently displayed tiles/items when a new layer is selected in the drop down menu.
        /// </summary>
        [ServerOrClient]
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
            
            /*foreach (GhostManager bg in _buildGhosts)
            {
                bg.gameObject.Dispose(true);
            }
            _buildGhosts.Clear();*/
            _buildGhosts.Add(CreateGhost(GhostManager.BuildMatMode.Valid, genericObjectSo.prefab, GetMousePosition(), Quaternion.identity));
        }

        private GhostManager CreateGhost(GhostManager.BuildMatMode mode, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GhostManager buildGhost = Instantiate(prefab, position, rotation).AddComponent<GhostManager>();
            buildGhost.SetupMaterials(_validConstruction, _invalidConstruction, _deleteConstruction);
            buildGhost.ChangeGhostColor(mode);
            return buildGhost;
        }
        private Vector3 GetMousePosition(bool isTilePosition = false)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (_plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                if (isTilePosition)
                {
                    return TileHelper.GetClosestPosition(point);
                }
                else
                {
                    return point;
                }
            }
            return Vector3.zero;
        }
    }
}