using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Electricity;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Base class for stuff that should connect in an electric circuit. 
    /// </summary>
    public abstract class ElectricAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// The placed object for this disposal pipe.
        /// </summary>
        protected PlacedTileObject PlacedObject;
        protected bool Initialized;

        protected virtual void Setup()
        {
            if (!Initialized)
            {
                PlacedObject = GetComponent<PlacedTileObject>();
                Initialized = true;
            }
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            return ElectricNeighbourLookup.GetNeighbours(PlacedObject);
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            return neighbourObject?.Connector is ElectricAdjacencyConnector;
        }

        public abstract void UpdateAllConnections();

        public abstract bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);
    }

    /// <summary>
    /// Shared neighbour discovery for electric connectors (same-tile devices plus cardinal layer neighbours).
    /// </summary>
    internal static class ElectricNeighbourLookup
    {
        public static List<PlacedTileObject> GetNeighbours(PlacedTileObject placedObject)
        {
            if (placedObject == null)
                return new List<PlacedTileObject>();

            var neighbours = new HashSet<PlacedTileObject>();
            foreach (PlacedTileObject neighbour in GetElectricDevicesOnSameTile(placedObject))
            {
                neighbours.Add(neighbour);
            }

            foreach (PlacedTileObject neighbour in GetNeighbourElectricDevicesOnSameLayer(placedObject))
            {
                neighbours.Add(neighbour);
            }

            foreach (PlacedTileObject neighbour in ElectricCableConnectivity.GetCableLinkedDevices(placedObject))
            {
                neighbours.Add(neighbour);
            }

            neighbours.Remove(placedObject);
            neighbours.RemoveWhere(x => x == null);
            return neighbours.ToList();
        }

        private static List<PlacedTileObject> GetElectricDevicesOnSameTile(PlacedTileObject placedObject)
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem.CurrentMap;

            List<PlacedTileObject> devicesOnSameTile = new();

            TileChunk currentChunk = map.GetChunk(placedObject.gameObject.transform.position);
            List<ITileLocation> deviceLocations = currentChunk.GetTileLocations(placedObject.Origin.x, placedObject.Origin.y);

            foreach (ITileLocation location in deviceLocations)
            {
                foreach (PlacedTileObject tileObject in location.GetAllPlacedObject())
                {
                    if (tileObject.gameObject.TryGetComponent(out IElectricDevice device))
                        devicesOnSameTile.Add(tileObject);
                }
            }

            devicesOnSameTile.Remove(placedObject);

            return devicesOnSameTile;
        }

        private static List<PlacedTileObject> GetNeighbourElectricDevicesOnSameLayer(PlacedTileObject placedObject)
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem.CurrentMap;
            IEnumerable<PlacedTileObject> electricNeighbours = map.GetCardinalNeighbourPlacedObjects(placedObject.Layer,
                placedObject.gameObject.transform.position).Where(x => x != null && x.gameObject.TryGetComponent(out IElectricDevice device));

            return electricNeighbours.ToList();
        }
    }
}
