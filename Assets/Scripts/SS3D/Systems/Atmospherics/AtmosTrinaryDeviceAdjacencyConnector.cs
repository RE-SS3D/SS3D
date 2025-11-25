using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    [RequireComponent(typeof(TrinaryAtmosDevice))]
    public class AtmosTrinaryDeviceAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// The Placed tile object linked to this connector. It's this placed object that this
        /// connector update.
        /// </summary>
        private PlacedTileObject _placedObject;

        /// <summary>
        /// A structure containing data regarding connection of this PlacedTileObject with all 8
        /// adjacent neighbours (cardinal and diagonal connections).
        /// </summary>
        private AdjacencyMap _pipeLayerConnections;

        /// <summary>
        /// Upon Setup, this should stay true.
        /// </summary>
        private bool _initialized;

        public AdjacencyMap Connections => _pipeLayerConnections;

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool isUpdated = _pipeLayerConnections.SetConnection(dir, isConnected);

            if (isUpdated && neighbourObject && updateNeighbour)
            {
                neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
            }

            return isUpdated;
        }

        public void UpdateAllConnections()
        {
            Setup();

            List<PlacedTileObject> neighbourObjects = GetNeighbours();

            foreach (PlacedTileObject neighbourObject in neighbourObjects)
            {
                _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction dir);
                UpdateSingleConnection(dir, neighbourObject, true);
            }
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (!neighbourObject)
            {
                return false;
            }

            Vector2Int twoDForward = new((int)gameObject.transform.forward.x, (int)gameObject.transform.forward.z);
            Vector2Int twoDRight = new((int)gameObject.transform.right.x, (int)gameObject.transform.right.z);

            bool isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
            isConnected &= neighbourObject.GenericType == TileObjectGenericType.Pipe;
            isConnected &= neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin + twoDForward
                || neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin - twoDForward
                || neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin + twoDRight;

            // Only connect if not already connected to something else.
            isConnected &= _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction neighbourDirection)
                && !_pipeLayerConnections.HasConnection(neighbourDirection);

            return isConnected;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            if (!_initialized)
            {
                Setup();
            }

            TileSubSystem tileSubSystem = Subsystems.Get<TileSubSystem>();
            TileMap map = tileSubSystem.CurrentMap;
            List<PlacedTileObject> neighbours = new();
            List<PlacedTileObject> neighbourPipeLeft = map.GetCardinalNeighbourPlacedObjects(TileLayer.PipeLeft, _placedObject.gameObject.transform.position).ToList();
            List<PlacedTileObject> neighbourPipeMiddle = map.GetCardinalNeighbourPlacedObjects(TileLayer.PipeMiddle, _placedObject.gameObject.transform.position).ToList();
            List<PlacedTileObject> neighbourPipeRight = map.GetCardinalNeighbourPlacedObjects(TileLayer.PipeRight, _placedObject.gameObject.transform.position).ToList();
            List<PlacedTileObject> neighbourPipeSurface = map.GetCardinalNeighbourPlacedObjects(TileLayer.PipeSurface, _placedObject.gameObject.transform.position).ToList();

            neighbourPipeLeft.RemoveAll(x => x == null);
            neighbourPipeMiddle.RemoveAll(x => x == null);
            neighbourPipeRight.RemoveAll(x => x == null);
            neighbourPipeSurface.RemoveAll(x => x == null);

            neighbours.AddRange(neighbourPipeLeft);
            neighbours.AddRange(neighbourPipeMiddle);
            neighbours.AddRange(neighbourPipeRight);
            neighbours.AddRange(neighbourPipeSurface);

            return neighbours;
        }

        public List<PlacedTileObject> GetConnectedNeighbours() => GetNeighbours().Where(IsConnected).ToList();

        /// <summary>
        /// Simply set things up, including creating new references, and fetching generic and specific type
        /// from the associated scriptable object.
        /// </summary>
        private void Setup()
        {
            if (_initialized)
            {
                return;
            }

            _pipeLayerConnections = new AdjacencyMap();
            _placedObject = GetComponentInParent<PlacedTileObject>();
            _initialized = true;
        }
    }
}
