using UnityEditor;
using UnityEngine;
using SS3D.Systems.Crafting;
using System.Collections.Generic;
using QuikGraph;
using System.Linq;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Codice.Client.BaseCommands.TubeClient;
using System;
using QuikGraph.Algorithms;

public class CraftingRecipeEditor : EditorWindow
{
    private CraftingRecipe _recipe;

    private float _repulsiveConstant = 1;

    private float _attractiveConstant = 1;

    private float _idealLenght = 80;

    private int _maxIteration = 300;

    private float _delta = 1f;

    private AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> _graphWithPosition;

    private class RecipeStepWithPosition
    {
        public RecipeStep step;
        public Vector2 position;
    }

    [MenuItem("Window/SS3D/Crafting Recipe Display")]
    public static void ShowWindow()
    {
        GetWindow<CraftingRecipeEditor>("Crafting Recipe Display");
    }

    private void OnEnable()
    {
        // Try to get the GraphData from the selected object in the Inspector
        _recipe = Selection.activeObject as CraftingRecipe;

        _graphWithPosition = InitializeGraphWithPositions();

        SpringEmbedderAlgorithm(_graphWithPosition);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Recipe display", EditorStyles.boldLabel);

        if (GUILayout.Button("draw graph"))
        {
            _graphWithPosition = InitializeGraphWithPositions();

            EditorCoroutineUtility.StartCoroutine(SpringEmbedderAlgorithm(_graphWithPosition), this);
        }

        DrawGraph(_graphWithPosition);

        _repulsiveConstant = EditorGUILayout.Slider("Repulsive constant", _repulsiveConstant, 0, 100);
        _attractiveConstant = EditorGUILayout.Slider("Attractive constant", _attractiveConstant, 0, 100);
        _idealLenght = EditorGUILayout.Slider("Ideal lenght", _idealLenght, 0, 800);
        _delta = EditorGUILayout.Slider("Delta", _delta, 0, 20);
        _maxIteration = Math.Max(100, EditorGUILayout.IntField("Max iteration", _maxIteration));

    }

    private AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> InitializeGraphWithPositions()
    {
        AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graphWithPosition = new();

        // make a new graph giving positions to vertices.
        foreach (var edge in _recipe.RecipeGraph.Edges)
        {
            RecipeStepWithPosition source = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Source);
            RecipeStepWithPosition target = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Target);

            var edgeWithPosition = new TaggedEdge<RecipeStepWithPosition, RecipeStepLink>(
                source != null ? source : new RecipeStepWithPosition() { step = edge.Source, position = GetRandomCirclePosition() },
                target != null ? target : new RecipeStepWithPosition() { step = edge.Target, position = GetRandomCirclePosition() },
                edge.Tag
                );
            graphWithPosition.AddVerticesAndEdge(edgeWithPosition);
        }

        return graphWithPosition;
    }

    private void DrawGraph(AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graphWithPosition)
    {
        // draw the graph
        foreach (var stepWithPosition in graphWithPosition.Vertices)
        {
            Handles.DrawWireDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, 10f);
        }

        foreach (TaggedEdge<RecipeStepWithPosition, RecipeStepLink> edge in graphWithPosition.Edges)
        {
            Handles.DrawLine(edge.Source.position, edge.Target.position);
        }
    }

    private Vector2 GetRandomCirclePosition()
    {
        return UnityEngine.Random.insideUnitCircle * 300 + new Vector2(400, 400);
    }

    private IEnumerator SpringEmbedderAlgorithm(AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graph)
    {
        
        Tuple<RecipeStepWithPosition, Vector2>[] forcesOnVertices = ComputeForceAllVertices(graph);

        int t = 0;

        while (t < _maxIteration)
        {

            forcesOnVertices = ComputeForceAllVertices(graph);

            Vector2 forcesSum = forcesOnVertices.Select(x => x.Item2).Aggregate(Vector2.zero,
                (current, next) => current + next,
                result => result);

            Debug.Log(forcesSum);

            for(int j =0; j< forcesOnVertices.Length; j++)
            {
                forcesOnVertices[j].Item1.position += _delta * forcesOnVertices[j].Item2;
            }

            t++;
            yield return null;
            Repaint();
        }
    }

    private Tuple<RecipeStepWithPosition,Vector2>[] ComputeForceAllVertices (AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graph)
    {
        Tuple<RecipeStepWithPosition, Vector2>[] forcesOnVertice = new Tuple<RecipeStepWithPosition, Vector2>[graph.VertexCount];
        int i = 0;
        foreach(var vertice in graph.Vertices)
        {
            forcesOnVertice[i] = new Tuple<RecipeStepWithPosition, Vector2>(vertice, ComputeForceSingleVertice(vertice, graph));
            i++;
        }

        return forcesOnVertice;
    }

    private Vector2 ComputeForceSingleVertice(RecipeStepWithPosition vertice,
        AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graph)
    {
        // Issue, should not be only out edges, but also "in" edges.
        // Idea : go trough the list of edges, keep all target or source vertice when current vertice is the edge.

        List<RecipeStepWithPosition> conVertices = new List<RecipeStepWithPosition>();

        foreach(var edge in graph.Edges)
        {
            if(edge.Target == vertice && edge.Source != vertice) 
            {
                conVertices.Add(edge.Source);
            }
            else if(edge.Source== vertice && edge.Target != vertice)
            {
                conVertices.Add(edge.Target);
            }
        }

        IEnumerable<RecipeStepWithPosition> connectedVertices = graph.OutEdges(vertice).Select(x => x.Target);
        List<RecipeStepWithPosition> currentVertice = new List<RecipeStepWithPosition>() { vertice };

        IEnumerable<RecipeStepWithPosition> unconnectedVertices = graph.Vertices.Except(currentVertice) ;

        //var notOutVertices = graph.Edges.Except(outVertices);
        Vector2 attractive = ComputeAttractiveComponent(vertice, conVertices);
        Vector2 repulsive = ComputeRepulsiveComponent(vertice, unconnectedVertices);

        return attractive + repulsive;
    }

    private Vector2 ComputeAttractiveComponent(RecipeStepWithPosition vertice, IEnumerable<RecipeStepWithPosition> vertices)
    {
        Vector2 result = Vector2.zero;
        
        foreach (var target in vertices)
        {
            result += AttractiveForce(vertice.position, target.position);
        }

        return result;
    }

    private Vector2 ComputeRepulsiveComponent(RecipeStepWithPosition vertice, IEnumerable<RecipeStepWithPosition> vertices)
    {
        Vector2 result = Vector2.zero;
        foreach (var target in vertices)
        {
            result += RepulsiveForce(vertice.position, target.position);
        }

        return result;
    }

    private Vector2 RepulsiveForce(Vector2 v1, Vector2 v2)
    {
        return (_repulsiveConstant/(Vector2.Distance(v1,v2) * Vector2.Distance(v1, v2))) * (v1 - v2).normalized;
    }

    private Vector2 AttractiveForce(Vector2 v1, Vector2 v2)
    {
        return (_attractiveConstant * Mathf.Log(Vector2.Distance(v1, v2)) / _idealLenght)* (v2 - v1).normalized;
    }
}
