using Coimbra;
using DynamicPanels;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.UI;
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
        private Direction _direction = Direction.North;

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
            _controls.Rotate.performed += HandleRotate;
        }

        private void HandleRotate(InputAction.CallbackContext context)
        {
            _direction = TileHelper.GetNextCardinalDir(_direction);
            foreach (GhostManager buildGhost in _buildGhosts)
            {
                buildGhost.SetRotation(_direction);
            }
        }

        private void HandlePlaceStarted(InputAction.CallbackContext context)
        {
            if (_itemPlacement)
                return;
            
            _isDragging = true;
            _dragStartPostion = GetMousePosition(true);
        }

        private void HandlePlacePerformed(InputAction.CallbackContext context)
        {
            _isDragging = false;

            if (!_isDeleting)
            {
                bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
                foreach (GhostManager buildGhost in _buildGhosts)
                {
                    _tileSystem.RpcPlaceObject(_selectedObject.nameString, buildGhost.TargetPosition, buildGhost.Dir, isReplacing);
                }
            }
            else
            {
                if (_itemPlacement)
                {
                    FindAndDeleteItem();
                }
                else
                {
                    foreach (GhostManager buildGhost in _buildGhosts)
                    {
                        _tileSystem.RpcClearTileObject(_selectedObject.nameString, buildGhost.TargetPosition, buildGhost.Dir);
                    }
                }
            }
            ClearGhosts();

            if (_selectedObject == null)
                return;
                
            _buildGhosts.Add(CreateGhost(_selectedObject.prefab, GetMousePosition(!_itemPlacement), _direction));
        }
        private void FindAndDeleteItem()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                PlacedItemObject placedItem = hitInfo.collider.gameObject.GetComponent<PlacedItemObject>();
                Debug.Log(placedItem.NameString);
                if (placedItem != null)
                {
                    _tileSystem.RpcClearItemObject(placedItem.NameString, placedItem.gameObject.transform.position);
                }
            }
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
            
            Vector3 position = GetMousePosition(!_itemPlacement);
            if (_buildGhosts.Count == 1)
            {
                _buildGhosts.First().TargetPosition = position;
                RefreshGhost(_buildGhosts.First());
            }

            if (_isDragging && (position != _lastSnappedPosition) && (_selectedObject != null))
            {
                ClearGhosts();

                if (_controls.SquareDrag.phase == InputActionPhase.Performed)
                {
                    int x1 = (int)Math.Min(_dragStartPostion.x, position.x);
                    int x2 = (int)Math.Max(_dragStartPostion.x, position.x);
                    int y1 = (int)Math.Min(_dragStartPostion.z, position.z);
                    int y2 = (int)Math.Max(_dragStartPostion.z, position.z);
                    Debug.Log(x1.ToString() + " " + x2.ToString() + " " + y1.ToString() + " " + y2.ToString());

                    for (int i = y1; i <= y2; i++)
                    {
                        for (int j = x1; j <= x2; j++)
                        {
                            Vector3 tile =  new(j, 0, i);
                            GhostManager buildGhost = CreateGhost(_selectedObject.prefab, tile, _direction);
                            buildGhost.TargetPosition = tile;
                            RefreshGhost(buildGhost);
                            _buildGhosts.Add(buildGhost);
                        }
                    }
                }
                else
                {
                    foreach (Vector3 tile in FindTilesOnLine(_dragStartPostion, position))
                    {
                        GhostManager buildGhost = CreateGhost(_selectedObject.prefab, tile, _direction);
                        buildGhost.TargetPosition = tile;
                        RefreshGhost(buildGhost);
                        _buildGhosts.Add(buildGhost);
                    }
                }
            }
            _lastSnappedPosition = position;
        }

        private void RefreshGhost(GhostManager buildGhost)
        {
            GhostManager.BuildMatMode mode;
            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;

            if (_isDeleting)
            {
                mode = GhostManager.BuildMatMode.Delete;
            }
            else
            {
                switch (_itemPlacement)
                {
                    case true:
                        mode = GhostManager.BuildMatMode.Valid;
                        break;

                    case false when _tileSystem.CanBuild((TileObjectSo)_selectedObject, buildGhost.TargetPosition, buildGhost.Dir, isReplacing):
                        mode = GhostManager.BuildMatMode.Valid;
                        break;

                    case false:
                        mode = GhostManager.BuildMatMode.Invalid;
                        break;
                } 
            }
            buildGhost.ChangeGhostColor(mode);
        }
        private List<Vector3> FindTilesOnLine(Vector3 firstPoint, Vector3 secondPoint)
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
                ClearGhosts();
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
                Instantiate(_slotPrefab, _contentRoot.transform, true).GetComponent<TileMapCreatorTab>().Setup(asset);
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
            ClearGhosts();
            _buildGhosts.Add(CreateGhost(genericObjectSo.prefab, GetMousePosition(!_itemPlacement), _direction));
        }

        private GhostManager CreateGhost(GameObject prefab, Vector3 position, Direction direction)
        {
            Quaternion rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(direction), 0);
            GhostManager buildGhost = Instantiate(prefab, position, rotation).AddComponent<GhostManager>();
            buildGhost.SetupMaterials(_validConstruction, _invalidConstruction, _deleteConstruction);
            GhostManager.BuildMatMode mode;
            if (_isDeleting)
            {
                mode = GhostManager.BuildMatMode.Delete;
            }
            else
            {
                mode = GhostManager.BuildMatMode.Valid;
            }
            buildGhost.ChangeGhostColor(mode);
            buildGhost.SetRotation(direction);
            return buildGhost;
        }

        private void ClearGhosts()
        {
            for (int i = 0; i < _buildGhosts.Count; )
            {
                _buildGhosts[i].gameObject.Dispose(true);
                _buildGhosts.RemoveAt(i);
            }
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