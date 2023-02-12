using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Tile;
using SS3D.Core;
using UnityEngine.UIElements;

namespace SS3D.Systems.Construction.UI
{
    public class ConstructionMenu : MonoBehaviour
    {
        public GameObject _menuRoot;
        public GameObject _contentRoot;
        public GameObject _slotPrefab;

        private bool _enabled = false;
        private bool _initalized = false;
        private bool _itemPlacement = false;
        private Vector3 _lastSnappedPosition;
        private int _placeIndex;
        private Direction _dir;
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
                LoadTileGrid();

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
                CheckBuildValidity(_lastSnappedPosition);
            }


            // Check if mouse moved
            Vector3 snappedPosition = TileHelper.GetClosestPosition(GetMousePosition());
            if (snappedPosition != _lastSnappedPosition)
            {
                CheckBuildValidity(snappedPosition);
                _ghostManager.SetTargetPosition(snappedPosition);
                _lastSnappedPosition = snappedPosition;
            }

            // Move ghost
            _ghostManager.MoveGhost();

            // Check if we can want to place an object
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                _tileSystem.PlaceTileObject(_tileDatabase[_placeIndex], snappedPosition, _dir);
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
            bool valid = _tileSystem.CanBuild(_tileDatabase[_placeIndex], placePosition, _dir);

            if (valid)
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Valid);
            else
                _ghostManager.ChangeGhostColor(ConstructionHelper.BuildMatMode.Invalid);
        }

        private void ShowUI(bool show)
        {
            _menuRoot.SetActive(show);
        }

        private void LoadTileGrid()
        {
            _tileDatabase = _tileSystem.GetLoader().GetAllTileAssets();

            foreach (var asset in _tileDatabase)
            {
                GameObject slot = Instantiate(_slotPrefab);
                slot.transform.SetParent(_contentRoot.transform);

                ConstructionTab tab = slot.AddComponent<ConstructionTab>();
               
                tab.Setup(asset);
            }
        }
    }
}