using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    public class DirectionnalAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {

        protected TileObjectGenericType _genericType;


        protected TileObjectSpecificType _specificType;

        
        public DirectionnalShapeResolver AdjacencyResolver;

        protected AdjacencyMap _adjacencyMap;

        protected MeshFilter _filter;

        protected PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject => _placedObject;

        public Direction PlacedObjectDirection => _placedObject.Direction;

        private bool _initialized;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();
        }

        /// <summary>
        /// Simply set things up, including creating new references, and fetching generic and specific type
        /// from the associated scriptable object.
        /// </summary>
        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponent<PlacedTileObject>();
                if (_placedObject == null)
                {
                    _genericType = TileObjectGenericType.None;
                    _specificType = TileObjectSpecificType.None;
                }
                else
                {
                    _genericType = _placedObject.GenericType;
                    _specificType = _placedObject.SpecificType;
                }
                _initialized = true;
            }
        }

        /// <summary>
        /// directionnal are connected only with other directionnals on their right or on their left.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="neighbourObject"></param>
        /// <returns></returns>
        public bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            if (neighbourObject == null) return false;

            if (!TileHelper.CardinalDirections().Contains(dir)) return false;

            if (_adjacencyMap.CardinalConnectionCount == 2) return false;

            // add a check to see if connected to another neighbour object instead of the cardinal connection count.

            bool isConnected = true;

            if (neighbourObject.IsInFront(_placedObject) || neighbourObject.IsBehind(_placedObject))
            {
                isConnected &=  neighbourObject.Direction != _placedObject.Direction &&
                    neighbourObject.Direction != TileHelper.GetOpposite(_placedObject.Direction);
            }

            if (neighbourObject.IsOnLeft(_placedObject) || neighbourObject.IsOnRight(_placedObject))
            {
                isConnected &= neighbourObject.Direction != TileHelper.GetOpposite(_placedObject.Direction);
            }

            isConnected &= neighbourObject.GenericType == _genericType;
            isConnected &= neighbourObject.SpecificType == _specificType;

            return isConnected;
        }


        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            Setup();

            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i], true);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(dir, neighbourObject);

            bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));

            // Update our neighbour as well
            if (isConnected && updateNeighbour)
                neighbourObject.UpdateSingleAdjacency(_placedObject, TileHelper.GetOpposite(dir));

            if (isUpdated)
            {
                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        protected void UpdateMeshAndDirection()
        {

            Tuple<Direction, MeshDirectionInfo> info;

            var tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;
            
            var neighbours = map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);


            List<Direction> neighboursFacingDirection = new();

            // TODO not solid code if order of neighbours gets changed
            foreach (Direction dir in TileHelper.CardinalDirections())
            {
                if(_adjacencyMap.HasConnection(dir))
                {
                    neighboursFacingDirection.Add(neighbours[(int)dir].Direction);
                }
            }

            info = AdjacencyResolver.GetMeshAndDirectionAndRotation(_adjacencyMap, PlacedObjectDirection, neighboursFacingDirection);

            _placedObject.SetDirection(info.Item1);

            if (_filter == null)
            {
                Log.Warning(this, "Missing mesh {meshDirectionInfo}", Logs.Generic, info);
            }

            _filter.mesh = info.Item2.Mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, info.Item2.Rotation, eulerRotation.z);

            transform.localRotation = localRotation;
        }
    }


}

