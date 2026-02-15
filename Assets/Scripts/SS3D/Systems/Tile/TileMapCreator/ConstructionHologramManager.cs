using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for managing tile objects, that will be used for building or deleting by the tile map menu.
    /// It handles creating construction holograms, replacing, adding or deleting tile objects and items upon placement,
    /// and placing the holograms in cool shapes like lines and squares.
    /// </summary>
    public class ConstructionHologramManager : NetworkActor
    {
        /// <summary>
        /// The last direction registered by a build ghost.
        /// </summary>
        private Direction _lastRegisteredDirection;
        private InputSubSystem _inputSystem;
        private Controls.TileCreatorActions _controls;
        private bool _isPlacingItem = false;
        /// <summary>
        /// Snapped position are positions in the center of tiles, to display tile objects ghosts properly.
        /// </summary>
        private Vector3 _lastSnappedPosition;
        /// <summary>
        /// The snapped position of the mouse, in the middle of a tile, when the player starts dragging with the mouse.
        /// </summary>
        private Vector3 _dragStartPostion;
        /// <summary>
        /// Is the player currently dragging ?
        /// </summary>
        public bool IsDragging => _isDragging;
        private bool _isDragging;
        private GenericObjectSo _selectedObject;
        /// <summary>
        /// List of build ghosts currently displaying in game.
        /// </summary>
        private List<ConstructionHologram> _holograms = new();
        [SerializeField]
        private TileMapMenuSubSystem _menu;

        public void SetSelectedObject(GenericObjectSo genericObjectSo)
        {
            _isPlacingItem = genericObjectSo switch
            {
                TileObjectSo => false,
                ItemObjectSo => true,
                _ => _isPlacingItem,
            };
            _selectedObject = genericObjectSo;

            DestroyHolograms();
            CreateHologram(genericObjectSo.prefab, TileHelper.GetPointedPosition(!_isPlacingItem));
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            
            _controls.Place.started += HandlePlaceStarted;
            _controls.Place.performed += HandlePlacePerformed;
            _controls.Replace.performed += HandleReplace;
            _controls.Replace.canceled += HandleReplace;
            _controls.Rotate.performed += HandleRotate;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            
            _controls.Place.started -= HandlePlaceStarted;
            _controls.Place.performed -= HandlePlacePerformed;
            _controls.Replace.performed -= HandleReplace;
            _controls.Replace.canceled -= HandleReplace;
            _controls.Rotate.performed -= HandleRotate;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_holograms.Count == 1)
            {
                _holograms[0].UpdateRotationAndPosition();
            }

            ActivateGhosts();

            Vector3 position = TileHelper.GetPointedPosition(!_isPlacingItem);
            // Move hologram, that sticks to the mouse. Currently it exists only if player is not dragging.
            if (_holograms.Count == 1)
            {
                _holograms.First().TargetPosition = position;
                if (position != _lastSnappedPosition)
                {
                    RefreshHologram(_holograms.First());
                }
            }

            if (_isDragging && (position != _lastSnappedPosition) && (_selectedObject != null))
            {
                Vector3[] tiles;
                if (_controls.SquareDrag.phase == InputActionPhase.Performed)
                {
                    tiles = SquareDrag(position);
                }
                else
                {
                    tiles = LineDrag(position);
                }

                int difference = _holograms.Count - tiles.Length;
                if (_holograms.Count > tiles.Length)
                {
                    for (int i = 0; i < difference; i++)
                    {
                        _holograms[0].Destroy();
                        _holograms.RemoveAt(0);
                    }
                }
                else
                {
                    for (int i = 0; i < -difference; i++)
                    {
                        CreateHologram(_selectedObject.prefab, new());
                    }
                }

                for (int i = 0; i < tiles.Length; i++)
                {
                    _holograms[i].Hologram.transform.position = tiles[i];
                    _holograms[i].TargetPosition = tiles[i];
                    RefreshHologram(_holograms[i]);
                }
            }
            _lastSnappedPosition = position;
        }

        /// <summary>
        /// Rotate all existing ghosts in the next allowed rotation.
        /// </summary>
        public void SetNextRotation()
        {
            foreach (ConstructionHologram hologram in _holograms)
            {
                hologram.SetNextRotation();
                RefreshHologram(hologram);
            }
            _lastRegisteredDirection = _holograms.First().Direction;
        }

        /// <summary>
        /// Instantiate in the correct position and rotation a single hologram.
        /// </summary>
        public ConstructionHologram CreateHologram(GameObject prefab, Vector3 position)
        {
            GameObject tileObject = Instantiate(prefab);
            ConstructionHologram hologram = new(tileObject, position, _lastRegisteredDirection);
            tileObject.transform.rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(hologram.Direction), 0);
            tileObject.transform.position = hologram.TargetPosition;
            _holograms.Add(hologram);
            RefreshHologram(hologram);
            return hologram;
        }

        /// <summary>
        /// Destroy all existing holograms.
        /// </summary>
        public void DestroyHolograms()
        {
            for (int i = _holograms.Count - 1; i >= 0; i--)
            {
                _holograms[i].Destroy();
            }
            _holograms.Clear();
        }

        /// <summary>
        /// Called upon control triggered to rotate holograms.
        /// </summary>
        private void HandleRotate(InputAction.CallbackContext context)
        {
            SetNextRotation();
        }

        /// <summary>
        /// This is called when the player start dragging hologramss.
        /// </summary>
        private void HandlePlaceStarted(InputAction.CallbackContext context)
        {
            // Dragging is disabled for items
            if (_isPlacingItem)
                return;

            _isDragging = true;
            _dragStartPostion = TileHelper.GetPointedPosition(true);
        }

        /// <summary>
        /// Method called when the tile objects are placed or deleted.
        /// </summary>
        private void HandlePlacePerformed(InputAction.CallbackContext context)
        {
            _isDragging = false;

            if (_menu.MouseOverUI)
            {
                _inputSystem.ToggleAction(_controls.Place, false);
            }

            if (!_menu.IsDeleting)
            {
                PlaceOnHolograms();
            }
            else
            {
                DeleteOnHolograms();
            }

            DestroyHolograms();

            if (_selectedObject == null) return;
            CreateHologram(_selectedObject.prefab, TileHelper.GetPointedPosition(!_isPlacingItem));
        }

        /// <summary>
        /// Called when the control is triggered to replace already present tile objects with new ones.
        /// </summary>
        /// <param name="context"></param>
        private void HandleReplace(InputAction.CallbackContext context)
        {
            foreach (ConstructionHologram buildGhost in _holograms)
            {
                RefreshHologram(buildGhost);
            }
        }

        /// <summary>
        /// Place all objects on the tilemap that are at the same locations as existing holograms.
        /// </summary>
        private void PlaceOnHolograms()
        {
            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;

            foreach (ConstructionHologram buildGhost in _holograms)
            {
                SubSystems.Get<TileSubSystem>().RpcPlaceObject(_selectedObject.NameString, buildGhost.TargetPosition, buildGhost.Direction, isReplacing);
            }
        }

        /// <summary>
        /// Delete all objects, that are at the same locations as existing holograms.
        /// </summary>
        private void DeleteOnHolograms()
        {
            if (_isPlacingItem)
            {
                FindAndDeleteItem();
            }
            else
            {
                foreach (ConstructionHologram hologram in _holograms)
                {
                    SubSystems.Get<TileSubSystem>().RpcClearTileObject(_selectedObject.NameString, hologram.TargetPosition, hologram.Direction);
                }
            }
        }

        /// <summary>
        /// Update material of holograms based build (or anything else) mode and holograms position  
        /// </summary>
        private void RefreshHologram(ConstructionHologram hologram)
        {
            if (_menu.IsDeleting)
            {
                hologram.ChangeHologramColor(ConstructionMode.Delete);
            }
            else if (_isPlacingItem)
            {
                hologram.ChangeHologramColor(ConstructionMode.Valid);
            }
            else
            {
                bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
                RpcSendCanBuild(_selectedObject.NameString, hologram.TargetPosition, hologram.Direction, isReplacing, LocalConnection);
            }
        }

        /// <summary>
        /// Activate all buildGhosts. This method is important, because for some reason network objects disable themselves after a few frames.
        /// </summary>
        private void ActivateGhosts()
        {
            foreach (ConstructionHologram buildGhost in _holograms)
            {
                if (!buildGhost.ActiveSelf)
                {
                    buildGhost.SetActive = true;
                }
            }
        }

        /// <summary>
        /// Starting from a given position, create holograms along a line defined by dragging. 
        /// </summary>
        private Vector3[] LineDrag(Vector3 position)
        {
            Vector2 firstPoint = new(_dragStartPostion.x, _dragStartPostion.z);
            Vector2 secondPoint = new(position.x, position.z);
            Vector3[] tiles = MathUtility.FindTilesOnLine(firstPoint, secondPoint)
                .Select(x => new Vector3(x.x, position.y, x.y)).ToArray();

            return tiles;
        }
        /// <summary>
        /// Create a square of objects holograms.
        /// </summary>
        /// <param name="position"> Fist position of the square</param>
        private Vector3[] SquareDrag(Vector3 position)
        {
            int x1 = (int)Math.Min(_dragStartPostion.x, position.x);
            int x2 = (int)Math.Max(_dragStartPostion.x, position.x);
            int y1 = (int)Math.Min(_dragStartPostion.z, position.z);
            int y2 = (int)Math.Max(_dragStartPostion.z, position.z);

            List<Vector3> tiles = new();
            
            for (int i = y1; i <= y2; i++)
            {
                for (int j = x1; j <= x2; j++)
                {
                    tiles.Add(new (j, position.y, i));
                }
            }

            return tiles.ToArray();
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RpcSendCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting, NetworkConnection conn)
        {

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();

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
            // Find correct hologram to update its material
            for (int i = 0; i < _holograms.Count; i++)
            {
                ConstructionHologram hologram = _holograms[i];
                if (hologram.TargetPosition != placePosition) continue;

                if (canBuild)
                {
                    hologram.ChangeHologramColor(ConstructionMode.Valid);
                }
                else
                {
                    hologram.ChangeHologramColor(ConstructionMode.Invalid);
                }
                return;
            }
        }

        /// <summary>
        /// Method called when trying to delete an item from the tilemap (as opposed to a tile object).
        /// Items need a special method because they are not tied to specific coordinates like tile objects.
        /// </summary>
        private void FindAndDeleteItem()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                PlacedItemObject placedItem = hitInfo.collider.gameObject.GetComponent<PlacedItemObject>();
                if (placedItem != null)
                {
                    SubSystems.Get<TileSubSystem>().RpcClearItemObject(placedItem.NameString, placedItem.gameObject.transform.position);
                }
            }
        }
    }
}