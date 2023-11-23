using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static SS3D.Systems.Tile.TileMapCreator.BuildGhostManager;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for managing ghost objects, that will be used for building or deleting by TileMapCreator.
    /// It handles creating construction holograms, replacing, adding or deleting tile objects and items upon placement,
    /// and placing the holograms in cool shapes like lines and squares.
    /// </summary>
    public class BuildGhostManager : NetworkActor
    {
        /// <summary>
        /// The last direction registered by a build ghost.
        /// </summary>
        private Direction _lastRegisteredDirection;

        private InputSystem _inputSystem;
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


        private bool _isDragging;

        /// <summary>
        /// Is the player currently dragging ?
        /// </summary>
        public bool IsDragging => _isDragging;


        private GenericObjectSo _selectedObject;

        /// <summary>
        /// List of build ghosts currently displaying in game.
        /// </summary>
        public List<BuildGhost> _ghosts = new();

        [SerializeField]
        private TileMapMenu _menu;

        public void SetSelectedObject(GenericObjectSo genericObjectSo)
        {
            _isPlacingItem = genericObjectSo switch
            {
                TileObjectSo => false,
                ItemObjectSo => true,
                _ => _isPlacingItem,
            };
            _selectedObject = genericObjectSo;

            DestroyGhosts();
            CreateGhost(genericObjectSo.prefab, TileHelper.GetPointedPosition(!_isPlacingItem));
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            _inputSystem = Subsystems.Get<InputSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            _controls.Place.started += HandlePlaceStarted;
            _controls.Place.performed += HandlePlacePerformed;
            _controls.Replace.performed += HandleReplace;
            _controls.Replace.canceled += HandleReplace;
            _controls.Rotate.performed += HandleRotate;
        }


        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            foreach (var buildGhost in _ghosts)
            {
                buildGhost.UpdateRotationAndPosition();
            }

            ActivateGhosts();

            Vector3 position = TileHelper.GetPointedPosition(!_isPlacingItem);
            // Move buildGhost, that sticks to the mouse. Currently it exists only if player is not dragging.
            if (_ghosts.Count == 1)
            {
                _ghosts.First().Position = position;
                if (position != _lastSnappedPosition)
                {
                    RefreshGhost(_ghosts.First());
                }
            }

            if (_isDragging && (position != _lastSnappedPosition) && (_selectedObject != null))
            {
                // Delete all ghosts and instantiate new on correct positions. Currently it causes large fps drops.
                DestroyGhosts();
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

        /// <summary>
        /// Rotate all existing ghosts in the next allowed rotation.
        /// </summary>
        public void SetNextRotation()
        {
            foreach (var ghost in _ghosts)
            {
                ghost.SetNextRotation();
            }
            _lastRegisteredDirection = _ghosts.First().Direction;
        }

        /// <summary>
        /// Instantiate in the correct position and rotation a single build ghost.
        /// </summary>
        public BuildGhost CreateGhost(GameObject prefab, Vector3 position)
        {
            var _ghostObject = Instantiate(prefab);
            var buildGhost = new BuildGhost(_ghostObject, position, _lastRegisteredDirection);
            _ghostObject.transform.rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(buildGhost.Direction), 0);
            _ghostObject.transform.position = buildGhost.Position;
            _ghosts.Add(buildGhost);
            return buildGhost;
        }

        /// <summary>
        /// Destroy all existing ghosts.
        /// </summary>
        public void DestroyGhosts()
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                _ghosts[i].Destroy();
            }
            _ghosts.Clear();
        }

        /// <summary>
        /// Called upon control triggered to rotate build ghosts.
        /// </summary>
        private void HandleRotate(InputAction.CallbackContext context)
        {
            SetNextRotation();
        }

        /// <summary>
        /// This is called when the player start dragging build ghosts.
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
                PlaceOnGhosts();
            }
            else
            {
                DeleteOnGhosts();
            }

            DestroyGhosts();

            if (_selectedObject == null) return;
            CreateGhost(_selectedObject.prefab, TileHelper.GetPointedPosition(!_isPlacingItem));
        }

        /// <summary>
        /// Called when the control is triggered to replace already present tile objects with new ones.
        /// </summary>
        /// <param name="context"></param>
        private void HandleReplace(InputAction.CallbackContext context)
        {
            foreach (BuildGhost buildGhost in _ghosts)
            {
                RefreshGhost(buildGhost);
            }
        }

        /// <summary>
        /// Place all objects on the tilemap that are at the same locations as existing build ghosts.
        /// </summary>
        private void PlaceOnGhosts()
        {
            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;

            foreach (BuildGhost buildGhost in _ghosts)
            {
                Subsystems.Get<TileSystem>().RpcPlaceObject(_selectedObject.nameString, buildGhost.Position, buildGhost.Direction, isReplacing);
            }
        }

        /// <summary>
        /// Delete all objects, that are at the same locations as existing build ghosts.
        /// </summary>
        private void DeleteOnGhosts()
        {
            if (_isPlacingItem)
            {
                FindAndDeleteItem();
            }
            else
            {
                foreach (BuildGhost buildGhost in _ghosts)
                {
                    Subsystems.Get<TileSystem>().RpcClearTileObject(_selectedObject.nameString, buildGhost.Position, buildGhost.Direction);
                }
            }
        }

        /// <summary>
        /// Update material of buildGhost based build (or anything else) mode and ghosts position  
        /// </summary>
        private void RefreshGhost(BuildGhost buildGhost)
        {
            if (_menu.IsDeleting)
            {
                buildGhost.ChangeGhostColor(BuildMatMode.Delete);
            }
            else if (_isPlacingItem)
            {
                buildGhost.ChangeGhostColor(BuildMatMode.Valid);
            }
            else
            {
                bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
                RpcSendCanBuild(_selectedObject.nameString, buildGhost.Position, buildGhost.Direction, isReplacing, LocalConnection);
            }
        }

        /// <summary>
        /// Activate all buildGhosts. This method is important, because for some reason network objects disable themselves after a few frames.
        /// </summary>
        private void ActivateGhosts()
        {
            foreach (BuildGhost buildGhost in _ghosts)
            {
                if (!buildGhost.ActiveSelf)
                {
                    buildGhost.SetActive = true;
                }
            }
        }

        /// <summary>
        /// Starting from a given position, create tile ghosts along a line defined by dragging. 
        /// </summary>
        private void LineDrag(Vector3 position)
        {
            Vector2 firstPoint = new(_dragStartPostion.x, _dragStartPostion.z);
            Vector2 secondPoint = new(position.x, position.z);
            Vector3[] tiles = MathUtility.FindTilesOnLine(firstPoint, secondPoint)
                .Select(x => new Vector3(x.x, position.y, x.y)).ToArray();

            foreach (Vector3 tile in tiles)
            {
                var ghost = CreateGhost(_selectedObject.prefab, tile);
                RefreshGhost(ghost);
            }
        }


        [ServerRpc(RequireOwnership = false)]
        private void RpcSendCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting, NetworkConnection conn)
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
            for (int i = 0; i < _ghosts.Count; i++)
            {
                BuildGhost buildGhost = _ghosts[i];
                if (buildGhost.Position != placePosition) continue;

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

        /// <summary>
        /// Create a square of objects holograms.
        /// </summary>
        /// <param name="position"> Fist position of the square</param>
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
                    BuildGhost buildGhost = CreateGhost(_selectedObject.prefab, tile);
                    RefreshGhost(buildGhost);
                }
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
                    Subsystems.Get<TileSystem>().RpcClearItemObject(placedItem.NameString, placedItem.gameObject.transform.position);
                }
            }
        }



    }
}