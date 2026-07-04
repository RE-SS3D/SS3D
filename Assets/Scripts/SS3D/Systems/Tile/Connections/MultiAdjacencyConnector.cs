using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Add this script on any game objects that need multiple adjacency connectors, such as girders.
    /// Note that it means that those connectors won't behave completely independently.
    /// Should always be put on the "root" game object, at the same level as the placed tile object script.
    /// </summary>
    public class MultiAdjacencyConnector : Actor, IAdjacencyConnector, IEngineDrivenAdjacency
    {
        /// <summary>
        /// Game objects that hold a IAdjacencyConnector component.
        /// </summary>
        [SerializeField]
        private List<GameObject> _connectors;

        public IConnectionRule ConnectionRule
        {
            get
            {
                PlacedTileObject placed = GetComponent<PlacedTileObject>();
                return new SimpleConnectionRule(placed.GenericType, placed.SpecificType);
            }
        }

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
            foreach (GameObject connectorObject in _connectors)
            {
                if (connectorObject != null && connectorObject.TryGetComponent(out IEngineDrivenAdjacency engineDriven))
                    engineDriven.SetAdjacencyConnections(horizontalConnections);
            }
        }

        /// <summary>
        /// Return the neighbours of all connectors.
        /// </summary>
        public List<PlacedTileObject> GetNeighbours()
        {
            List<PlacedTileObject> neighbours = new();

            foreach (GameObject connector in _connectors)
            {
                neighbours.AddRange(connector.GetComponent<IAdjacencyConnector>()?.GetNeighbours());
            }
            return neighbours;
        }

        /// <summary>
        /// If any of the connectors is connected, return true, else return false.
        /// </summary>
        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            return _connectors.Any(x => x.GetComponent<IAdjacencyConnector>()?.IsConnected(neighbourObject) == true);
        }

        public void UpdateAllConnections()
        {
            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(GetComponent<PlacedTileObject>());
            map.AdjacencyEngine.ProcessQueue();
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null)
                return false;

            PlacedTileObject placed = GetComponent<PlacedTileObject>();
            map.AdjacencyEngine.QueueUpdate(placed);

            if (updateNeighbour && neighbourObject != null && neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                map.AdjacencyEngine.QueueUpdate(neighbourObject);

            map.AdjacencyEngine.ProcessQueue();
            return true;
        }
    }
}
