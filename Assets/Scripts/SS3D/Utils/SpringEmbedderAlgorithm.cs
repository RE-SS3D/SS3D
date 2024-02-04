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

    public static class SpringEmbedderAlgorithm<TVertex, TEdge, TTag>
    {

        private static float RepulsiveConstant = 1;

        private static float AttractiveConstant = 1;

        private static float IdealLenght = 80;

        private static float Delta = 1f;

        private static float ForceToStop = 0.1f;

        public static void SetParameters(float repulsive, float attractive, float idealLenght, float delta, float forceToStop)
        {
            RepulsiveConstant= repulsive;
            AttractiveConstant= attractive;
            IdealLenght = idealLenght;
            ForceToStop = forceToStop;
            Delta = delta;
        }

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

        private static Vector2 GetRandomCirclePosition()
        {
            return UnityEngine.Random.insideUnitCircle * 300 + new Vector2(400, 400);
        }

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

        private static Vector2 ComputeForceSingleVertice(VerticeWithPosition<TVertex> vertice,
            AdjacencyGraph<VerticeWithPosition<TVertex>, TaggedEdge<VerticeWithPosition<TVertex>, TTag>> graph)
        {
            // Issue, should not be only out edges, but also "in" edges.
            // Idea : go trough the list of edges, keep all target or source vertice when current vertice is the edge.

            List<VerticeWithPosition<TVertex>> conVertices = new List<VerticeWithPosition<TVertex>>();

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

            //var notOutVertices = graph.Edges.Except(outVertices);
            Vector2 attractive = ComputeAttractiveComponent(vertice, conVertices);
            Vector2 repulsive = ComputeRepulsiveComponent(vertice, unconnectedVertices);

            return attractive + repulsive;
        }

        private static Vector2 ComputeAttractiveComponent(VerticeWithPosition<TVertex> vertice, IEnumerable<VerticeWithPosition<TVertex>> vertices)
        {
            Vector2 result = Vector2.zero;

            foreach (VerticeWithPosition<TVertex> target in vertices)
            {
                result += AttractiveForce(vertice.position, target.position);
            }

            return result;
        }

        private static Vector2 ComputeRepulsiveComponent(VerticeWithPosition<TVertex> vertice, IEnumerable<VerticeWithPosition<TVertex>> vertices)
        {
            Vector2 result = Vector2.zero;
            foreach (VerticeWithPosition<TVertex> target in vertices)
            {
                result += RepulsiveForce(vertice.position, target.position);
            }

            return result;
        }

        private static Vector2 RepulsiveForce(Vector2 v1, Vector2 v2)
        {
            return (RepulsiveConstant / (Vector2.Distance(v1, v2) * Vector2.Distance(v1, v2))) * (v1 - v2).normalized;
        }

        private static Vector2 AttractiveForce(Vector2 v1, Vector2 v2)
        {
            return (AttractiveConstant * Mathf.Log(Vector2.Distance(v1, v2)) / IdealLenght) * (v2 - v1).normalized;
        }


    }
}
