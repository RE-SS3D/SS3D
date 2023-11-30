using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;
using Coimbra.Services.PlayerLoopEvents;
using Coimbra.Services.Events;

namespace System.Electricity
{
    /// <summary>
    /// This system should handle a big graph containing all circuits.
    /// When removing electrical elements or adding new ones, it should not immediately update, instead, it should mark the graph dirty and
    /// trigger an update. Imagine an explosion destroying 247 circuit elements in a single frame. It if update upon any change, it would
    /// have to update the whole graph 247 times. Instead, each time an element is removed, it should tell it to the electricity system, and
    /// each frame, if an element or more has changed, the graph can update.
    /// </summary>
    public class ElectricitySystem : NetworkSystem
    {

        private bool _graphIsDirty;

        private List<Circuit> _circuits;


        // Start is called before the first frame update
        protected override void OnStart()
        {
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            var graph = new AdjacencyGraph<int, Edge<int>>();

            graph.AddVertex(0);
            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);

            graph.AddEdge(new Edge<int>(0,1));
            graph.AddEdge(new Edge<int>(0, 2));
            graph.AddEdge(new Edge<int>(1, 2));
            graph.AddEdge(new Edge<int>(1, 3));
            graph.AddEdge(new Edge<int>(2, 3));

            foreach (int vertex in graph.Vertices)
            {
                foreach (Edge<int> edge in graph.OutEdges(vertex))
                {
                    Debug.Log(edge);
                }
            }
        }

        // Update is called once per frame
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {

        }

        public void AddElectricalElement(IElectricDevice device)
        {
            
        }

        public void RemoveElectricalElement(IElectricDevice device)
        {

        }
    }
}
