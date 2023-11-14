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
        private bool _isDeleting = false;
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

        [SerializeField]
        private Material _validConstruction;
        [SerializeField]
        private Material _invalidConstruction;
        [SerializeField]
        private Material _deleteConstruction;

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
            if (!_buildGhosts.IsNullOrEmpty())
            {
                _buildGhosts.Last().TargetPosition = GetMousePosition();
                Debug.Log(_buildGhosts.Last().gameObject.activeSelf);
            }
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
            GameObject model = Instantiate(genericObjectSo.prefab, GetMousePosition(), Quaternion.identity);
            model.SetActive(true);
            GhostManager buildGhost = model.AddComponent<GhostManager>();
            Debug.Log(buildGhost.gameObject.activeSelf);
            buildGhost.SetupMaterials(_validConstruction, _invalidConstruction, _deleteConstruction);
            _buildGhosts.Add(buildGhost);
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
    }
}