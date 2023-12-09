using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;
using Coimbra.Services.PlayerLoopEvents;
using Coimbra.Services.Events;
using QuikGraph.Algorithms;
using SS3D.Systems.Tile;
using SS3D.Utils;
using log4net.Util;
using SS3D.Core;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Electricity
{
    /// <summary>
    /// This system should handle a big graph containing all circuits.
    /// When removing electrical elements or adding new ones, it should not immediately update, instead, it should mark the graph dirty and
    /// trigger an update. Imagine an explosion destroying 247 circuit elements in a single frame. It if update upon any change, it would
    /// have to update the whole graph 247 times. Instead, each time an element is removed, it should tell it to the electricity system, and
    /// each tick, if an element or more has changed, the graph can update.
    /// </summary>
    public class ElectricitySystem : NetworkSystem
    {
        /// <summary>
        /// Register on this if the system is not set up yet.
        /// </summary>
        public event Action OnSystemSetUp;

        /// <summary>
        /// Called each time the electricity system update. All things that need to sync with the electricity updates should suscribe
        /// to this event instead of performing their logics in Unity's update loops.
        /// </summary>
        public event Action OnTick;

        /// <summary>
        /// If the system is properly set up, do stuff with it.
        /// </summary>
        public bool IsSetUp { get; private set; }

        /// <summary>
        /// Little struct to have a unique set of number for any electric device on the map. 
        /// The coordinates are the position on the map (x and y), the layer and the direction of the electric device.
        /// None two tile objects should have the same set of coordinates.
        /// </summary>
        private struct VerticeCoordinates
        {
            public short x;
            public short y;
            public byte layer;
            public byte direction;

            public VerticeCoordinates(short xcoordinate, short yccordinate, byte zcoordinate, byte directionCoordinate)
            {
                x = xcoordinate;
                y = yccordinate;
                layer = zcoordinate;
                direction = directionCoordinate;
            }
        }

        /// <summary>
        /// The graph is considered dirty if there was any changes during the previous frame in the way electric devices are put on the map.
        /// </summary>
        private bool _graphIsDirty;


        /// <summary>
        /// List of all circuits on the map.
        /// </summary>
        private List<Circuit> _circuits;


        /// <summary>
        /// Graph representing all electric devices and connections on the map. each electric device is a vertice in this graph,
        /// and each electric connection is an edge.
        /// </summary>
        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;

        /// <summary>
        /// Interval between two ticks in seconds.
        /// </summary>
        [SerializeField]
        private float _tickRate = 0.2f;

        private float _timeElapsed = 0f;


        protected override void OnStart()
        {       
            _electricityGraph = new UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>>();

            _circuits = new List<Circuit>();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));

            IsSetUp = true;

            OnSystemSetUp?.Invoke();

            OnTick += HandleCircuitsUpdate;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            _timeElapsed += Time.deltaTime;

            if(_timeElapsed> _tickRate)
            {
                OnTick?.Invoke();
                _timeElapsed -= _tickRate;
            }
        }

        private void HandleCircuitsUpdate()
        {
            if (_graphIsDirty)
            {
                _graphIsDirty = false;
                UpdateAllCircuitsTopology();
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
        public void AddElectricalElement(IElectricDevice device)
        {
            VerticeCoordinates deviceCoordinates = new VerticeCoordinates((short)device.TileObject.WorldOrigin.x, (short)device.TileObject.WorldOrigin.y,
                    (byte)device.TileObject.Layer, (byte)device.TileObject.Direction);

            _electricityGraph.AddVertex
            (
                new VerticeCoordinates
                (
                    (short) device.TileObject.WorldOrigin.x,
                    (short) device.TileObject.WorldOrigin.y,
                    (byte) device.TileObject.Layer,
                    (byte) device.TileObject.Direction
                )
            );

            List<PlacedTileObject> neighbours = device.TileObject.Connector.GetNeighbours();

            foreach(PlacedTileObject neighbour in neighbours)
            {
                VerticeCoordinates neighbourCoordinates = new VerticeCoordinates ((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                    (byte)neighbour.Layer, (byte)neighbour.Direction);
                
                _electricityGraph.AddVertex(neighbourCoordinates);
                _electricityGraph.AddEdge(new Edge<VerticeCoordinates>(deviceCoordinates, neighbourCoordinates));
            }

            _graphIsDirty = true;

        }

        /// <summary>
        /// Remove an electric device from the map.
        /// </summary>
        /// <param name="device"> The device to remove.</param>
        public void RemoveElectricalElement(IElectricDevice device)
        {
            if (device == null || device.TileObject == null) return;

            _electricityGraph.RemoveVertex
            (
                new VerticeCoordinates
                (
                    (short)device.TileObject.WorldOrigin.x,
                    (short)device.TileObject.WorldOrigin.y,
                    (byte)device.TileObject.Layer,
                    (byte)device.TileObject.Direction
                )
            );

            _graphIsDirty = true;
        }

        /// <summary>
        /// Recompute all circuits, should be called when there's some changes on the map regarding 
        /// how electric devices are placed.
        /// TODO : When removing electrical elements, identify which circuits are affected and only update those.
        /// TODO : When adding electrical elements, use some kind of metric to determine which circuits might be affected by the adding, and only update those.
        ///        Maybe simply check which component gets connected to the new device, and update all circuits with those components.
        /// </summary>
        private void UpdateAllCircuitsTopology()
        {
            Dictionary<VerticeCoordinates, int> components = new();

            // Compute all connected components in the graph. One component is basically one circuit.
            // In graph theory, a component is a set of all vertices linked by at least one path.
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
                _circuits.Add(new Circuit());
                foreach (VerticeCoordinates coord in component)
                {
                    TileSystem tileSystem = Subsystems.Get<TileSystem>();
                    ITileLocation location = tileSystem.CurrentMap.GetTileLocation((TileLayer)coord.layer, new Vector3(coord.x, 0f, coord.y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.direction)) continue;

                    if (!placedObject.TryGetComponent<IElectricDevice>(out var device)) continue;

                    _circuits.Last().AddElectricDevice(device);
                }
            }
        }
    }
}
