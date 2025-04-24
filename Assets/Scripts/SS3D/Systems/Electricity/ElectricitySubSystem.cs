using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;
using Coimbra.Services.PlayerLoopEvents;
using Coimbra.Services.Events;
using QuikGraph.Algorithms;
using SS3D.Systems.Tile;
using SS3D.Core;
using System.Linq;
using FishNet.Object;
using SS3D.Systems.Tile.Connections;

namespace System.Electricity
{
    /// <summary>
    /// Handles a graph, that contains all electicity circuits.
    /// </summary>
    /// <remarks>
    /// When removing electrical elements or adding new ones, it doesn't immediately update, instead,
    /// it marks the graph dirty and rebuilds the graph on the next tick.
    /// </remarks>
    public class ElectricitySubSystem : NetworkSubSystem
    {
        public event Action OnSystemSetUp;

        /// <summary>
        /// Called each time the electricity system update. All things that need to sync with the electricity updates should suscribe
        /// to this event instead of performing their logics in Unity's update loops.
        /// </summary>
        public event Action OnTick;
        
        public bool IsSetUp { get; private set; }

        /// <summary>
        /// Record to have a unique set of number for any electric device on the map. 
        /// None two tile objects should have the same set of coordinates.
        /// </summary>
        private record VerticeCoordinates(short X, short Y, byte Layer, byte Direction);

        /// <summary>
        /// The graph is considered dirty if there was any changes during the previous frame in the way electric devices are put on the map.
        /// </summary>
        private bool _graphIsDirty;
        
        /// <summary>
        /// List of all circuits on the map.
        /// </summary>
        private List<Circuit> _circuits;

        /// <summary>
        /// Graph representing all electric devices and connections on the map. Each electric device is a vertice in this graph,
        /// and each electric connection is an edge.
        /// </summary>
        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;

        /// <summary>
        /// Interval between two ticks in seconds.
        /// </summary>
        [SerializeField]
        private float _tickRate = 0.2f;
        private float _timeElapsed = 0f;

        public override void OnStartServer()
        {       
            base.OnStartServer();
            _electricityGraph = new();
            _circuits = new();
            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
            IsSetUp = true;
            OnSystemSetUp?.Invoke();
            OnTick += HandleCircuitsUpdate;
        }

        [Server]
        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            _timeElapsed += Time.deltaTime;

            if(_timeElapsed > _tickRate)
            {
                RpcInvokeOnTick();
                _timeElapsed = 0;
            }
        }

        [Server]
        public void HandleCircuitsUpdate()
        {
            // Updating the graph on each change is unreliable in case of events, that change a lot of elements at one time, such as explosions.
            // Therefore, the graph updates each tick if it gets dirty.
            if (_graphIsDirty)
            {
                UpdateAllCircuitsTopology();
                _graphIsDirty = false;
            }

            foreach (Circuit circuit in _circuits)
            {
                circuit.UpdateCircuitPower();
            }
        }

        /// <summary>
        /// Add an electric device to the electic graph, setting up vertices and new connections if necessary.
        /// </summary>
        /// <param name="device"> The device to add.</param>
        [Server]
        public void AddElectricalElement(IElectricDevice device)
        {
            PlacedTileObject tileObject = device.TileObject;
            VerticeCoordinates deviceCoordinates = new ((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y,
                    (byte)tileObject.Layer, (byte)tileObject.Direction);
            _electricityGraph.AddVertex(deviceCoordinates);
            List<PlacedTileObject> neighbours = tileObject.Connector?.GetNeighbours();
            foreach(PlacedTileObject neighbour in neighbours)
            {
                VerticeCoordinates neighbourCoordinates = new ((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                    (byte)neighbour.Layer, (byte)neighbour.Direction);
                
                _electricityGraph.AddVertex(neighbourCoordinates);
                _electricityGraph.AddEdge(new (deviceCoordinates, neighbourCoordinates));
            }
            _graphIsDirty = true;
        }

        /// <summary>
        /// Remove an electric device from the map.
        /// </summary>
        /// <param name="device"> The device to remove.</param>
        [Server]
        public void RemoveElectricalElement(IElectricDevice device)
        {
            if (device == null || device.TileObject == null) return;
            _electricityGraph.RemoveVertex
            (
                new
                (
                    (short)device.TileObject.WorldOrigin.x,
                    (short)device.TileObject.WorldOrigin.y,
                    (byte)device.TileObject.Layer,
                    (byte)device.TileObject.Direction
                )
            );
            _graphIsDirty = true;
        }

        // TODO: When removing electrical elements, identify which circuits are affected and only update those.
        // TODO: When adding electrical elements, use some kind of metric to determine what circuits might be affected by the adding, and only update those.
        // TODO: Maybe simply check which component gets connected to the new device, and update all circuits with those components.
        /// <summary>
        /// Recompute all circuits, should be called when there are some changes on the map regarding how electric devices are placed.
        /// </summary>
        [Server]
        private void UpdateAllCircuitsTopology()
        {
            Dictionary<VerticeCoordinates, int> components = new();

            // Compute all components in the graph. One component is basically one circuit.
            // In graph theory, a component is a connected subgraph that is not part of any larger connected subgraph.
            _electricityGraph.ConnectedComponents(components);
            _circuits.Clear();
            // group all vertice coordinates by the component index they belong to.
            Dictionary<int, List<VerticeCoordinates>> graphs = components.GroupBy(pair => pair.Value)
                .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Key).ToList()
                );

            // Set up the circuits, with their respective storages, producers, and consumers of power.
            foreach (List<VerticeCoordinates> component in graphs.Values)
            {
                _circuits.Add(new());
                foreach (VerticeCoordinates coord in component)
                {
                    TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
                    ITileLocation location = tileSystem.CurrentMap.GetTileLocation((TileLayer)coord.Layer, new(coord.X, 0f, coord.Y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.Direction)) continue;

                    if (!placedObject.TryGetComponent(out IElectricDevice device)) continue;

                    _circuits.Last().AddElectricDevice(device);
                }
            }
        }

        [ObserversRpc]
        private void RpcInvokeOnTick()
        {
            OnTick?.Invoke();
        }
    }
}
