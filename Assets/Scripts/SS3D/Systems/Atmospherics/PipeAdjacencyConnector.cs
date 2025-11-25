using Serilog;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Simple connector for pipes with a possible offset, such as atmos pipes.
    /// </summary>
    public class PipeAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        [FormerlySerializedAs("_connector")]
        [SerializeField]
        private OffsetPipeConnector _pipeConnector;

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

        private Direction _machineryConnection;

        private bool _isConnectedToMachinery;

        /// <summary>
        /// The specific mesh this connectable has.
        /// </summary>
        private MeshFilter _filter;

        /// <summary>
        /// Upon Setup, this should stay true.
        /// </summary>
        private bool _initialized;

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool isUpdated = false;

            // If neighbour is a atmos machinery and this pipe is connected to it
            if (isConnected && neighbourObject.TryGetComponent(out TrinaryAtmosDevice _))
            {
                isUpdated = !_isConnectedToMachinery;
                _isConnectedToMachinery = true;
                _placedObject.NeighbourAtDirectionOf(neighbourObject, out _machineryConnection);
            }

            if (isConnected && neighbourObject.TryGetComponent(out PipeAdjacencyConnector _))
            {
                isUpdated = _pipeLayerConnections.SetConnection(dir, true);
            }

            if (!isConnected && _pipeLayerConnections.HasConnection(dir))
            {
                isUpdated = _pipeLayerConnections.SetConnection(dir, false);
            }

            if (!isConnected && _isConnectedToMachinery && dir == _machineryConnection)
            {
                _isConnectedToMachinery = false;
                isUpdated = true;
            }

            if (isUpdated)
            {
                if (neighbourObject && updateNeighbour)
                {
                    neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
                }

                UpdateMeshAndDirection();
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
            bool isConnected = true;
            PlacedTileObject tileObject = GetComponent<PlacedTileObject>();

            if (neighbourObject == null || tileObject == null)
            {
                return false;
            }

            _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction neighbourDirection);
            Vector2Int neighbourObjectTwoDForward = new((int)neighbourObject.transform.forward.x, (int)neighbourObject.transform.forward.z);
            Vector2Int neighbourObjectTwoDRight = new((int)neighbourObject.transform.right.x, (int)neighbourObject.transform.right.z);

            isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;

            if (neighbourObject.TryGetComponent(out IAtmosValve valve))
            {
                isConnected &= tileObject.WorldOrigin == neighbourObject.WorldOrigin + neighbourObjectTwoDForward
                    || tileObject.WorldOrigin == neighbourObject.WorldOrigin - neighbourObjectTwoDRight;

                isConnected &= valve.IsOpen;
            }

            if (neighbourObject.TryGetComponent(out AtmosTrinaryDeviceAdjacencyConnector filter))
            {
                // This pipe connects to TrinaryAtmosDevice only if it's placed behind, in front or on one of its side
                isConnected &= tileObject.WorldOrigin == neighbourObject.WorldOrigin + neighbourObjectTwoDForward
                    || tileObject.WorldOrigin == neighbourObject.WorldOrigin - neighbourObjectTwoDForward
                    || tileObject.WorldOrigin == neighbourObject.WorldOrigin + neighbourObjectTwoDRight;

                // Can't connect to machinery if pipe already connected to other machinery except if it's the same machinery
                isConnected &= !_isConnectedToMachinery || (_isConnectedToMachinery && neighbourDirection == _machineryConnection);

                // Can't connect if connected to more than one other pipe
                isConnected &= _pipeLayerConnections.ConnectionCount <= 1;

                // If connected to one pipe already, connect only if the neighbour pipe is opposite to the machinery.
                if (_pipeLayerConnections.ConnectionCount == 1)
                {
                    isConnected &= _pipeLayerConnections.GetSingleConnection() == TileHelper.GetOpposite(neighbourDirection);
                }

                // If the device is already connected does not connect
                isConnected &= !filter.Connections.HasConnection(TileHelper.GetOpposite(neighbourDirection));
            }

            if (
                neighbourObject.TryGetComponent(out PipeAdjacencyConnector neighbourConnector)
                && neighbourConnector._isConnectedToMachinery
                && neighbourConnector._machineryConnection != neighbourDirection)
            {
                isConnected &= false;
            }

            // Pipes connect only between themselves on the same layer.
            isConnected &= valve != null || filter || neighbourObject.Layer == _placedObject.Layer;

            return isConnected;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            TileSubSystem tileSubSystem = Subsystems.Get<TileSubSystem>();
            TileMap map = tileSubSystem.CurrentMap;
            List<PlacedTileObject> neighboursPipe = map.GetCardinalNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position).ToList();
            List<PlacedTileObject> neighboursMachinery = map.GetCardinalNeighbourPlacedObjects(TileLayer.Turf, _placedObject.transform.position).ToList();
            neighboursPipe.RemoveAll(x => !x);
            neighboursMachinery.RemoveAll(x => !x);
            neighboursPipe.AddRange(neighboursMachinery);
            return neighboursPipe;
        }

        public List<PlacedTileObject> GetConnectedNeighbours()
        {
            return GetNeighbours().Where(IsConnected).ToList();
        }

        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = _pipeConnector.GetMeshAndDirection(_pipeLayerConnections, _isConnectedToMachinery, _machineryConnection);
            _filter.mesh = info.Mesh;
            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);
            transform.localRotation = localRotation;
        }

        private void Setup()
        {
            if (_initialized)
            {
                return;
            }

            _filter = GetComponent<MeshFilter>();
            _placedObject = GetComponentInParent<PlacedTileObject>();
            _pipeLayerConnections = new AdjacencyMap();
            _initialized = true;
        }
    }
}
