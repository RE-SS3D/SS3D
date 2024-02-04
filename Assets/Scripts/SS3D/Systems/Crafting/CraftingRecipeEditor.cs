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
using UnityEditor.PackageManager.UI;

/// <summary>
/// Custom window to display the recipe graph of a recipe opened in inspector.
/// </summary>
public class CraftingRecipeEditor : EditorWindow
{
    private float _repulsiveConstant = 1;

    private float _attractiveConstant = 1;

    private float _idealLenght = 80;

    private int _maxIteration = 300;

    private float _delta = 1f;

    private float _forceToStop = 0.1f;

    private AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> _graphWithPosition;

    private const float KZoomMin = 0.1f;
    private const float KZoomMax = 10.0f;

    private readonly Rect _zoomArea = new Rect(200.0f, 200.0f, 1200.0f, 600.0f);
    private float _zoom = 1.0f;
    private Vector2 _zoomCoordsOrigin = Vector2.zero;

    private const float CircleSize = 5f;

    private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
    {
        return (screenCoords - _zoomArea.TopLeft()) / _zoom + _zoomCoordsOrigin;
    }

    private void DrawZoomArea()
    {
        // Within the zoom area all coordinates are relative to the top left corner of the zoom area
        // with the width and height being scaled versions of the original/unzoomed area's width and height.
        EditorZoomArea.Begin(_zoom, _zoomArea);

        GUILayout.BeginArea(new Rect(- _zoomCoordsOrigin.x, - _zoomCoordsOrigin.y, 1600.0f, 900.0f));

        DrawGraph(_graphWithPosition);

        GUILayout.EndArea();

        EditorZoomArea.End();
    }

    private void DrawNonZoomArea()
    {
        EditorGUILayout.LabelField("Recipe display", EditorStyles.boldLabel);

        if (GUILayout.Button("draw graph"))
        {
            CraftingRecipe recipe = Selection.activeObject as CraftingRecipe;

            if (recipe == null) return;

            _graphWithPosition = InitializeGraphWithPositions(recipe.RecipeGraph);

            EditorCoroutineUtility.StartCoroutine(SpringEmbedderAlgorithm(_graphWithPosition), this);
        }


        _zoom = EditorGUILayout.Slider("Zoom", _zoom, KZoomMin, KZoomMax);
        _repulsiveConstant = EditorGUILayout.Slider("Repulsive constant", _repulsiveConstant, 0, 1000);
        _attractiveConstant = EditorGUILayout.Slider("Attractive constant", _attractiveConstant, 0, 1000);
        _idealLenght = EditorGUILayout.Slider("Ideal lenght", _idealLenght, 0, 800);
        _delta = EditorGUILayout.Slider("Delta", _delta, 0, 50);
        _forceToStop = EditorGUILayout.Slider("Max force to stop", _forceToStop, 0.001f, 5);
        _maxIteration = Math.Max(100, EditorGUILayout.IntField("Max iteration", _maxIteration)); 
    }

    private void HandleEvents()
    {
        // Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
        // as the zoom center instead of the top left corner of the zoom area. This is achieved by
        // maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
        if (Event.current.type == EventType.ScrollWheel)
        {
            Vector2 screenCoordsMousePos = Event.current.mousePosition;
            Vector2 delta = Event.current.delta;
            Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
            float zoomDelta = -delta.y / 150.0f;
            float oldZoom = _zoom;
            _zoom += zoomDelta;
            _zoom = Mathf.Clamp(_zoom, KZoomMin, KZoomMax);
            _zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - (oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin);

            Event.current.Use();
        }

        // Allow moving the zoom area's origin by dragging with the middle mouse button or dragging
        // with the left mouse button with Alt pressed.
        if (Event.current.type == EventType.MouseDrag &&
            (Event.current.button == 0 && Event.current.modifiers == EventModifiers.Alt) ||
            Event.current.button == 2)
        {
            Vector2 delta = Event.current.delta;
            delta /= _zoom;
            _zoomCoordsOrigin += delta;

            Event.current.Use();
        }
    }

    private class RecipeStepWithPosition
    {
        public RecipeStep step;
        public Vector2 position;
    }

    [MenuItem("Window/SS3D/Crafting Recipe Display")]
    public static void ShowWindow()
    {
        CraftingRecipeEditor window = GetWindow<CraftingRecipeEditor>("Crafting Recipe Display");
        window.minSize = new Vector2(600.0f, 300.0f);
        window.wantsMouseMove = true;
    }

    private void OnGUI()
    {
        HandleEvents();
        // The zoom area clipping is sometimes not fully confined to the passed in rectangle. At certain
        // zoom levels you will get a line of pixels rendered outside of the passed in area because of
        // floating point imprecision in the scaling. Therefore, it is recommended to draw the zoom
        // area first and then draw everything else so that there is no undesired overlap.
        DrawZoomArea();
        DrawNonZoomArea();

    }

    /// <summary>
    /// Create a new graph very similar to the one passed in parameter, but enhance with position data, and initialize those positions
    /// in a random circle.
    /// </summary>
    /// <returns></returns>
    private AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> InitializeGraphWithPositions(
        AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> originalGraph)
    {
        AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graphWithPosition = new();

        // make a new graph giving positions to vertices.
        foreach (TaggedEdge<RecipeStep, RecipeStepLink> edge in originalGraph.Edges)
        {
            RecipeStepWithPosition source = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Source);
            RecipeStepWithPosition target = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Target);

            TaggedEdge<RecipeStepWithPosition, RecipeStepLink> edgeWithPosition = new TaggedEdge<RecipeStepWithPosition, RecipeStepLink>(
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
        foreach (RecipeStepWithPosition stepWithPosition in graphWithPosition.Vertices)
        {
            Color color = stepWithPosition.step.IsTerminal ? Color.red : stepWithPosition.step.IsInitialState ? Color.green : Color.gray;
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);
            Handles.color = Color.black;
            Handles.DrawWireDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);

            EditorGUI.LabelField(new Rect(stepWithPosition.position.x, stepWithPosition.position.y, 200, 20), stepWithPosition.step.Name);
        }
        Handles.color = Color.white;


        foreach (TaggedEdge<RecipeStepWithPosition, RecipeStepLink> edge in graphWithPosition.Edges)
        {
            Handles.DrawAAPolyLine(3, edge.Source.position, edge.Target.position);

            DrawArrowhead(edge.Source.position, edge.Target.position, 20, 8f);
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

            Vector2 maxForce = forcesOnVertices.Select(x => x.Item2).Aggregate(Vector2.zero,
                (current, next) => current.magnitude > next.magnitude ? current : next,
                result => result);

            if(maxForce.magnitude < _forceToStop) { break; }

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
        foreach(RecipeStepWithPosition vertice in graph.Vertices)
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

        foreach(TaggedEdge<RecipeStepWithPosition, RecipeStepLink> edge in graph.Edges)
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
        
        foreach (RecipeStepWithPosition target in vertices)
        {
            result += AttractiveForce(vertice.position, target.position);
        }

        return result;
    }

    private Vector2 ComputeRepulsiveComponent(RecipeStepWithPosition vertice, IEnumerable<RecipeStepWithPosition> vertices)
    {
        Vector2 result = Vector2.zero;
        foreach (RecipeStepWithPosition target in vertices)
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

    // Helper method to draw arrowhead
    private void DrawArrowhead(Vector2 start, Vector2 end, float arrowheadAngle, float arrowheadLength)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 arrowheadLeft = Quaternion.Euler(0, 0, arrowheadAngle) * -direction * arrowheadLength;
        Vector2 arrowheadRight = Quaternion.Euler(0, 0, -arrowheadAngle) * -direction * arrowheadLength;

        Handles.DrawAAPolyLine(3, new Vector3(end.x, end.y, 0), new Vector3(end.x + arrowheadLeft.x, end.y + arrowheadLeft.y, 0));
        Handles.DrawAAPolyLine(3, new Vector3(end.x, end.y, 0), new Vector3(end.x + arrowheadRight.x, end.y + arrowheadRight.y, 0));
    }
}

