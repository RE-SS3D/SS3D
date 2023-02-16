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
using SS3D.Logging;

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
        private GenericObjectSo _selectedObject;

        private TileSystem _tileSystem;
        private ConstructionHelper _ghostManager;
        private Plane _plane;

        private List<GenericObjectSo> _objectDatabase;

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

                LoadObjectGrid(new[] { TileLayer.Plenum }, false);

                _initalized = true;
            }
        }

        private void Update()
        {
            // Check for enabling the construction menu
            if (Input.GetKeyDown(KeyCode.B))
            {
                _enabled = !_enabled;
                ShowUI(_enabled);
                Initialize();
            }

            if (!_initalized)
                return;

            // Clean-up if we are not building
            if (!_enabled || _selectedObject == null)
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
            if (snappedPosition != _lastSnappedPosition && !_itemPlacement)
            {
                _ghostManager.SetTargetPosition(snappedPosition);
                _lastSnappedPosition = snappedPosition;
                RefreshGhost();
            }

            if (_itemPlacement)
            {
                Vector3 newPosition = GetMousePosition();
                _ghostManager.SetTargetPosition(newPosition);
                _lastSnappedPosition = newPosition;
            }

            // Move ghost
            _ghostManager.MoveGhost();

            // Check if we can want to place or delete an object
            if (Input.GetKeyDown(KeyCode.Mouse0) && !_mouseOverUI)
            {
                PlaceObjectClick(_lastSnappedPosition);
            }
        }

        private void PlaceObjectClick(Vector3 snappedPosition)
        {
            if (_isDeleting)
            {
                if (_itemPlacement)
                    DeleteItemObjectClick(snappedPosition);
                else
                    _tileSystem.RpcClearTileObject(_selectedObject.nameString, snappedPosition);
            }
            else
            {
                _tileSystem.RpcPlaceObject(_selectedObject.nameString, snappedPosition, _ghostManager.GetDir());
                RefreshGhost();
            }
        }

        private void DeleteItemObjectClick(Vector3 worldPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                PlacedItemObject placedItem = hitInfo.collider.gameObject.GetComponent<PlacedItemObject>();

                if (placedItem != null)
                {
                    _tileSystem.RpcClearItemObject(placedItem.GetNameString(), worldPosition);
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
            TileObjectSo tileObjectSo = (TileObjectSo) _tileSystem.GetAsset(tileObjectSoName);

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

        private void LoadObjectGrid(TileLayer[] allowedLayers, bool isItems)
        {
            ClearGrid();

            _objectDatabase = _tileSystem.GetLoader().GetAllAssets();

            foreach (var asset in _objectDatabase)
            {
                if (isItems && asset is not ItemObjectSo)
                    continue;

                if (!isItems && asset is ItemObjectSo)
                    continue;

                if (!isItems && asset is TileObjectSo && !allowedLayers.Contains(((TileObjectSo)asset).layer))
                    continue;

                GameObject slot = Instantiate(_slotPrefab);
                slot.transform.SetParent(_contentRoot.transform);

                ConstructionTab tab = slot.AddComponent<ConstructionTab>();
               
                tab.Setup(asset);
            }
        }

        private void ClearGrid()
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
            else if (_itemPlacement)
            {
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Valid);
            }
            else if (!_itemPlacement)
            {
                CheckBuildValidity(_lastSnappedPosition);
            }
        }

        public void SetSelectedObject(GenericObjectSo genericObjectSo)
        {
            if (genericObjectSo is TileObjectSo)
                _itemPlacement = false;
            else if (genericObjectSo is ItemObjectSo)
                _itemPlacement = true;

            _selectedObject = genericObjectSo;
            _ghostManager.DestroyGhost();
            _ghostManager.CreateGhost(genericObjectSo.prefab);
            RefreshGhost();
        }

        public void LoadMap()
        {
            if (IsServer)
                _tileSystem.Load();
            else
                Punpun.Say(this, "Only the server is allowed to load the map");
        }

        public void SaveMap()
        {
            if (IsServer)
                _tileSystem.Save();
            else
                Punpun.Say(this, "Only the server is allowed to save the map");
        }

        

        public void OnDropDownChange()
        {
            int index = _layerPlacementDropdown.value;

            switch (index)
            {
                case 0:
                    LoadObjectGrid(new[] {TileLayer.Plenum}, false);
                    break;
                case 1:
                    LoadObjectGrid(new[] { TileLayer.Turf }, false);
                    break;
                case 2:
                    LoadObjectGrid(new[] { TileLayer.FurnitureBase, TileLayer.FurnitureTop }, false);
                    break;
                case 3:
                    LoadObjectGrid(new[] { TileLayer.WallMountHigh, TileLayer.WallMountLow }, false);
                    break;
                case 4:
                    LoadObjectGrid(new[] { TileLayer.Wire, TileLayer.Disposal, TileLayer.Pipes }, false);
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

        public void SetIsDeleting(bool isDeleting)
        {
            if (_selectedObject != null)
            {
                _isDeleting = isDeleting;

                RefreshGhost();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOverUI = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
        }
    }
}