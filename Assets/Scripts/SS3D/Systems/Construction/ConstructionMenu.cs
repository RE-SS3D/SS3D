using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Tile;
using SS3D.Core;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System.Linq;
using TMPro;
using FishNet.Object;
using FishNet.Connection;

namespace SS3D.Systems.Construction.UI
{
    public class ConstructionMenu : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject _menuRoot;
        public GameObject _contentRoot;
        public GameObject _slotPrefab;
        public TMP_Dropdown _layerPlacementDropdown;

        private bool _enabled = false;
        private bool _initalized = false;
        private bool _isDeleting = false;
        private bool _itemPlacement = false;
        private bool _mouseOverUI = false;

        private Vector3 _lastSnappedPosition;
        private TileObjectSo _selectedObject;
        
        private TileSystem _tileSystem;
        private ConstructionHelper _ghostManager;
        private Plane _plane;

        private List<TileObjectSo> _tileDatabase;

        private void Start()
        {
            ShowUI(false);
        }

        private void Initialize()
        {
            if (!_initalized)
            {
                _tileSystem = SystemLocator.Get<TileSystem>();
                _ghostManager = GetComponent<ConstructionHelper>();
                _plane = new Plane(Vector3.up, 0);

                LoadFoundations();

                _initalized = true;
            }
        }

        private void Update()
        {
            // Check for enabling the construction menu
            if (Input.GetKeyDown(KeyCode.N))
            {
                _enabled = !_enabled;
                ShowUI(_enabled);
                Initialize();
            }

            if (!_initalized)
                return;

            // Clean-up if we are not building
            if (!_enabled || (_selectedObject == null))
            {
                _ghostManager.DestroyGhost();
                return;
            }
            else
            {
                _ghostManager.CreateGhost(_selectedObject.prefab);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _ghostManager.NextRotation();
                RefreshGhost();
            }


            // Check if mouse moved
            Vector3 snappedPosition = TileHelper.GetClosestPosition(GetMousePosition());
            if (snappedPosition != _lastSnappedPosition)
            {
                _ghostManager.SetTargetPosition(snappedPosition);
                _lastSnappedPosition = snappedPosition;
                RefreshGhost();
            }

            // Move ghost
            _ghostManager.MoveGhost();

            // Check if we can want to place or delete an object
            if (Input.GetKeyDown(KeyCode.Mouse0) && !_mouseOverUI)
            {
                if (_isDeleting)
                {
                    _tileSystem.RpcClearTileObject(_selectedObject.nameString, snappedPosition);
                }
                else
                {
                    _tileSystem.RpcPlaceTileObject(_selectedObject.nameString, snappedPosition, _ghostManager.GetDir());
                    RefreshGhost();
                }
            }
        }

        private Vector3 GetMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (_plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        private void CheckBuildValidity(Vector3 placePosition)
        {
            RpcSendCanBuild(_selectedObject.nameString, placePosition, _ghostManager.GetDir(), LocalConnection);
        }


        [ServerRpc(RequireOwnership = false)]
        public void RpcSendCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir, NetworkConnection con)
        {
            Debug.Log("Send a CanBuild packet");
            TileObjectSo tileObjectSo = _tileSystem.GetTileAsset(tileObjectSoName);

            bool canBuild = _tileSystem.CanBuild(tileObjectSo, placePosition, dir);
            RpcReceiveCanBuild(con, canBuild);
        }

        [TargetRpc]
        private void RpcReceiveCanBuild(NetworkConnection con, bool canBuild)
        {
            if (canBuild)
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Valid);
            else
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Invalid);
        }

        private void ShowUI(bool show)
        {
            _menuRoot.SetActive(show);
        }

        private void LoadTileGrid(TileLayer[] allowedLayers)
        {
            ClearTileGrid();

            _tileDatabase = _tileSystem.GetLoader().GetAllTileAssets();

            foreach (var asset in _tileDatabase)
            {
                if (!allowedLayers.Contains(asset.layer))
                    continue;

                GameObject slot = Instantiate(_slotPrefab);
                slot.transform.SetParent(_contentRoot.transform);

                ConstructionTab tab = slot.AddComponent<ConstructionTab>();
               
                tab.Setup(asset);
            }
        }

        private void ClearTileGrid()
        {
            for (int i = 0; i < _contentRoot.transform.childCount; i++)
            {
                Destroy(_contentRoot.transform.GetChild(i).gameObject);
            }
        }

        private void RefreshGhost()
        {
            if (_isDeleting)
            {
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Deleting);
            }
            else
            {
                CheckBuildValidity(_lastSnappedPosition);
            }
        }

        public void SetSelectedTile(TileObjectSo tileObjectSo)
        {
            _selectedObject = tileObjectSo;
            _ghostManager.DestroyGhost();
            _ghostManager.CreateGhost(tileObjectSo.prefab);
            RefreshGhost();
        }

        public void LoadMap()
        {
            _tileSystem.Load();
        }

        public void SaveMap()
        {
            _tileSystem.Save();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOverUI = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
        }

        public void OnDropDownChange()
        {
            int index = _layerPlacementDropdown.value;

            switch (index)
            {
                case 0: // Foundations
                    LoadFoundations();
                    break;
                case 1:
                    LoadFurniture();
                    break;
                default:
                    ClearTileGrid();
                    break;
            }
        }

        public void LoadFoundations()
        {
            TileLayer[] foundationLayers = new TileLayer[] { 
                TileLayer.Plenum, 
                TileLayer.Turf,
                TileLayer.Disposal,
                TileLayer.Pipes,
                TileLayer.Wire
            };

            LoadTileGrid(foundationLayers);
        }

        public void LoadFurniture()
        {
            TileLayer[] furnitureLayers = new TileLayer[] { 
                TileLayer.FurnitureBase, 
                TileLayer.FurnitureTop,
                TileLayer.WallMountHigh,
                TileLayer.WallMountLow,
            };
            LoadTileGrid(furnitureLayers);
        }

        public void SetIsDeleting(bool isDeleting)
        {
            if (_selectedObject != null)
            {
                _isDeleting = isDeleting;

                RefreshGhost();
            }
        }
    }
}