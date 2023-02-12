using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class TileBuilder : MonoBehaviour
    {

        public TileObjectSo[] tileDatabase;
        public ItemObjectSo[] itemDatabase;

        public Material validConstruction;
        public Material invalidConstruction;

        private int _placeIndex;
        private Direction _dir;
        private bool _isBuilding = false;
        private bool _itemMode = false;
        private TileSystem _tileSystem;
        private Plane _plane;

        private Vector3 _lastSnappedPosition;
        private GameObject _ghostObject;

        void Start()
        {
            _tileSystem = SystemLocator.Get<TileSystem>();
            _plane = new Plane(Vector3.up, 0);
            
        }

        private void CreateGhost()
        {
            if (!_itemMode)
            {
                _ghostObject = Instantiate(tileDatabase[_placeIndex].prefab);
            }
            else
            {
                _ghostObject = Instantiate(itemDatabase[_placeIndex].prefab);
                
            }

            var collider = _ghostObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            //if (InstanceFinder.ServerManager != null && _ghostObject.GetComponent<NetworkObject>() != null)
            //{
            //    InstanceFinder.ServerManager.Spawn(_ghostObject);
            //}

            ChangeGhostColor(false);
        }

        private void DestroyGhost()
        {
            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
            }
        }

        private void ChangeGhostColor(bool canBuild)
        {
            Material matToUse = canBuild ? validConstruction : invalidConstruction;

            foreach (MeshRenderer mr in _ghostObject.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = mr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = matToUse;
                }

                mr.materials = materials;
            }
            
            // _ghostObject.GetComponentInChildren<MeshRenderer>().materials = materials;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                _isBuilding = !_isBuilding;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _itemMode = !_itemMode;
                _placeIndex = 0;
                DestroyGhost();
            }

            if (_isBuilding != true)
            {
                DestroyGhost();
                return;
            }
                

            if (_ghostObject == null)
                CreateGhost();

            // Ugly hack since component is disabled by network manager
            _ghostObject.SetActive(true);
            Vector3 worldPosition;
            Vector3 snappedPosition = Vector3.zero;
            float distance;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            _ghostObject.transform.position = Vector3.Lerp(_ghostObject.transform.position, _lastSnappedPosition, Time.deltaTime * 15f);
            _ghostObject.transform.rotation = Quaternion.Lerp(_ghostObject.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(_dir), 0), Time.deltaTime * 15f);

            if (_plane.Raycast(ray, out distance))
            {
                worldPosition = ray.GetPoint(distance);

                if (!_itemMode)
                    snappedPosition = TileHelper.GetClosestPosition(worldPosition);
                else
                    snappedPosition = worldPosition;
            }

            if (snappedPosition != _lastSnappedPosition)
            {
                _lastSnappedPosition = snappedPosition;
                _ghostObject.SetActive(true);

                if (!_itemMode)
                {
                    if (_tileSystem.CanBuild(tileDatabase[_placeIndex], snappedPosition, _dir))
                    {
                        ChangeGhostColor(true);
                    }
                    else
                    {
                        ChangeGhostColor(false);
                    }
                }
                else
                {
                    ChangeGhostColor(true);
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                bool result = false;
                if (_itemMode)
                {
                    _tileSystem.PlaceItemObject(itemDatabase[_placeIndex], snappedPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(_dir), 0));
                }
                else
                {
                    result = _tileSystem.PlaceTileObject(tileDatabase[_placeIndex], snappedPosition, _dir);
                }

                if (result)
                {
                    Punpun.Say(this, "Placed object");
                    _ghostObject.SetActive(false);
                }
                else
                {
                    Punpun.Say(this, "Already occupied");
                }

            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                _dir = TileHelper.GetNextDir(_dir);
            }

            if (Input.GetKeyDown(KeyCode.J))
            {

                _tileSystem.Save();

                Punpun.Say(this, "Saved");
            }

            if (Input.GetKeyDown(KeyCode.K))
            {

                _tileSystem.Load();

                Punpun.Say(this, "Loaded");
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                DestroyGhost();
                _placeIndex++;

                if ((!_itemMode && _placeIndex >= tileDatabase.Length) ||
                    _itemMode && _placeIndex >= itemDatabase.Length)
                    _placeIndex = 0;

                Punpun.Say(this, "Switched to next item");
            }
        }
    }
}