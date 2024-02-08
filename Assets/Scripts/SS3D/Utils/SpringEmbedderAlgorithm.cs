using QuikGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Utils
{

    public class VerticeWithPosition<TVertex>
    {
        public TVertex vertice;
        public Vector2 position;
    }

    /// <summary>
    /// Inspired by this doc : https://i11www.iti.kit.edu/_media/teaching/winter2016/graphvis/graphvis-ws16-v6.pdf
    /// The spring embedder algorithm simulates the behavior of negative charged particles, linked by spring between
    /// themselves, and the shape it takes after it reaches its minimum energy leads to an easy-to-read graph.
    /// </summary>
    public static class SpringEmbedderAlgorithm<TVertex, TEdge, TTag>
    {
        /// <summary>
        /// How much vertices are repulsive to each other.
        /// </summary>
        private static float RepulsiveConstant = 1;

        /// <summary>
        /// How much vertices linked by an an edge attract each other.
        /// </summary>
        private static float AttractiveConstant = 1;

        /// <summary>
        /// Ideal lenght between vertices.
        /// </summary>
        private static float IdealLenght = 80;

        /// <summary>
        /// Delta acts like a "speed" factor for the algorithm, the higher it is, the faster it converges
        /// toward the solution, but values too high can lead to divergence.
        /// </summary>
        private static float Delta = 1f;

        /// <summary>
        /// Another criteria to stop the algorithm is what's the max force exerted on any vertices is at a given iteration.
        /// When lower than a given amount we consider it won't move much still, and we stop.
        /// </summary>
        private static float ForceToStop = 0.1f;

        /// <summary>
        /// Set all parameters of the algorithm, should be called 
        /// </summary>Z
        public static void SetParameters(float repulsive, float attractive, float idealLenght, float delta, float forceToStop)
        {
            RepulsiveConstant= repulsive;
            AttractiveConstant= attractive;
            IdealLenght = idealLenght;
            ForceToStop = forceToStop;
            Delta = delta;
        }

        /// <summary>
        /// Take any graph with tagged edges, and return an enhanced graph with a position for each edges.
        /// </summary>
        public static AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> InitializeGraphWithPositions(
            AdjacencyGraph<TVertex, TaggedEdge<TVertex, TTag>> originalGraph)
        {
            AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> graphWithPosition = new();

            // make a new graph giving positions to vertices.
            foreach (TaggedEdge<TVertex, TTag> edge in originalGraph.Edges)
            {
                VerticeWithPosition<TVertex> source = graphWithPosition.Vertices.FirstOrDefault(x => x.vertice.Equals(edge.Source));
                VerticeWithPosition<TVertex> target = graphWithPosition.Vertices.FirstOrDefault(x => x.vertice.Equals(edge.Target));



                TaggedEdge<VerticeWithPosition<TVertex>, TTag> edgeWithPosition = new TaggedEdge<VerticeWithPosition<TVertex>, TTag>(
                    source != null ? source : new VerticeWithPosition<TVertex>() { vertice = edge.Source, position = GetRandomCirclePosition() },
                    target != null ? target : new VerticeWithPosition<TVertex>() { vertice = edge.Target, position = GetRandomCirclePosition() },
                    edge.Tag
                    );
                graphWithPosition.AddVerticesAndEdge(edgeWithPosition);
            }

            return graphWithPosition;
        }

        /// <summary>
        /// Used to initialize randomly the position of vertices.
        /// </summary>
        private static Vector2 GetRandomCirclePosition()
        {
            return UnityEngine.Random.insideUnitCircle * 300 + new Vector2(400, 400);
        }

        /// <summary>
        /// Compute a sing step of the algorithm, return true if conditions are met to stop it.
        /// </summary>
        public static bool ComputeOneStep(
            AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> graphWithPosition)
        {

            Tuple<VerticeWithPosition<TVertex>, Vector2>[] forcesOnVertices = ComputeForceAllVertices(graphWithPosition);

            Vector2 maxForce = forcesOnVertices.Select(x => x.Item2).Aggregate(Vector2.zero,
                (current, next) => current.magnitude > next.magnitude ? current : next,
                result => result);

            if (maxForce.magnitude < ForceToStop) { return true; }

            for (int j = 0; j < forcesOnVertices.Length; j++)
            {
                forcesOnVertices[j].Item1.position += Delta * forcesOnVertices[j].Item2;
            }

            return false;
        }

        /// <summary>
        /// Compute the average force exerted on each vertices in the graph.
        /// </summary>
        private static Tuple<VerticeWithPosition<TVertex>, Vector2>[] ComputeForceAllVertices(
            AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> graph)
        {
            Tuple<VerticeWithPosition<TVertex>, Vector2>[] forcesOnVertice = new Tuple<VerticeWithPosition<TVertex>, Vector2>[graph.VertexCount];
            int i = 0;
            foreach (VerticeWithPosition<TVertex> vertice in graph.Vertices)
            {
                forcesOnVertice[i] = new Tuple<VerticeWithPosition<TVertex>, Vector2>(vertice, ComputeForceSingleVertice(vertice, graph));
                i++;
            }

            return forcesOnVertice;
        }

        /// <summary>
        /// Compute the average force exerted on a single vertice in the graph.
        /// </summary>
        private static Vector2 ComputeForceSingleVertice(VerticeWithPosition<TVertex> vertice,
            AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> graph)
        {
            List<VerticeWithPosition<TVertex>> conVertices = new List<VerticeWithPosition<TVertex>>();

            // Inefficient way of getting neighbour edges, but I could not find a better one.
            foreach (Edge<VerticeWithPosition<TVertex>> edge in graph.Edges)
            {
                if (edge.Target == vertice && edge.Source != vertice)
                {
                    conVertices.Add(edge.Source);
                }
                else if (edge.Source == vertice && edge.Target != vertice)
                {
                    conVertices.Add(edge.Target);
                }
            }

            List<VerticeWithPosition<TVertex>> currentVertice = new List<VerticeWithPosition<TVertex>>() { vertice };

            IEnumerable<VerticeWithPosition<TVertex>> unconnectedVertices = graph.Vertices.Except(currentVertice);

            Vector2 attractive = ComputeAttractiveComponent(vertice, conVertices);
            Vector2 repulsive = ComputeRepulsiveComponent(vertice, unconnectedVertices);

            return attractive + repulsive;
        }

        /// <summary>
        /// Compute the attractive average force exerted by vertices on a single vertice.
        /// </summary>
        private static Vector2 ComputeAttractiveComponent(VerticeWithPosition<TVertex> vertice, IEnumerable<VerticeWithPosition<TVertex>> vertices)
        {
            Vector2 result = Vector2.zero;

            foreach (VerticeWithPosition<TVertex> target in vertices)
            {
                result += AttractiveForce(vertice.position, target.position);
            }

            return result;
        }

        /// <summary>
        /// Compute the repulsive average force exerted by vertices on a single vertice.
        /// </summary>
        private static Vector2 ComputeRepulsiveComponent(VerticeWithPosition<TVertex> vertice, IEnumerable<VerticeWithPosition<TVertex>> vertices)
        {
            Vector2 result = Vector2.zero;
            foreach (VerticeWithPosition<TVertex> target in vertices)
            {
                result += RepulsiveForce(vertice.position, target.position);
            }

            return result;
        }

        /// <summary>
        /// Method to compute the repulsive force between two vertices.
        /// </summary>
        private static Vector2 RepulsiveForce(Vector2 v1, Vector2 v2)
        {
            return (RepulsiveConstant / (Vector2.Distance(v1, v2) * Vector2.Distance(v1, v2))) * (v1 - v2).normalized;
        }

        /// <summary>
        /// Method to compute the attractive force between two vertices.
        /// </summary>
        private static Vector2 AttractiveForce(Vector2 v1, Vector2 v2)
        {
            return (AttractiveConstant * Mathf.Log(Vector2.Distance(v1, v2)) / IdealLenght) * (v2 - v1).normalized;
        }


    }
}
