using SS3D.Core.Behaviours;
using System.Collections;
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
    public class MultiAdjacencyConnector : Actor, IAdjacencyConnector
    {
        /// <summary>
        /// Game objects that hold a IAdjacencyConnector component.
        /// </summary>
        [SerializeField]
        private List<GameObject> _connectors;

        /// <summary>
        /// Return the neighbours of all connectors.
        /// </summary>
        public List<PlacedTileObject> GetNeighbours()
        {
            List<PlacedTileObject> neighbours = new();

            foreach (var connector in _connectors)
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
            return _connectors.Any(x => (bool) x.GetComponent<IAdjacencyConnector>()?.IsConnected(neighbourObject));
        }

        public void UpdateAllConnections()
        {
            _connectors.ForEach(x => x.GetComponent<IAdjacencyConnector>()?.UpdateAllConnections());
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            _connectors.ForEach(x => x.GetComponent<IAdjacencyConnector>()?.UpdateSingleConnection(dir, neighbourObject, updateNeighbour));
            return true;
        }
    }
}
