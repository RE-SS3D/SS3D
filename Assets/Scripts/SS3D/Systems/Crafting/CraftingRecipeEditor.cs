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
using SS3D.Utils;
using UnityEngine.UIElements;
using SS3D.Substances;

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

    private AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> _graphWithPosition;

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

        if(_graphWithPosition != null) DrawGraph(_graphWithPosition);

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

            EditorCoroutineUtility.StartCoroutine(ComputeGraphPositions(recipe), this);
        }


        _zoom = EditorGUILayout.Slider("Zoom", _zoom, KZoomMin, KZoomMax);
        _repulsiveConstant = EditorGUILayout.Slider("Repulsive constant", _repulsiveConstant, 0, 1000);
        _attractiveConstant = EditorGUILayout.Slider("Attractive constant", _attractiveConstant, 0, 1000);
        _idealLenght = EditorGUILayout.Slider("Ideal lenght", _idealLenght, 0, 800);
        _delta = EditorGUILayout.Slider("Delta", _delta, 0, 50);
        _forceToStop = EditorGUILayout.Slider("Max force to stop", _forceToStop, 0.001f, 5);
        _maxIteration = Math.Max(100, EditorGUILayout.IntField("Max iteration", _maxIteration));
    }

    public IEnumerator ComputeGraphPositions(CraftingRecipe recipe)
    {
        _graphWithPosition =
    SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>.InitializeGraphWithPositions(recipe.RecipeGraph);

        int t = 0;

        bool forceReachedMinimum = false;

        while (t < _maxIteration)
        {
            SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>.SetParameters(_repulsiveConstant, _attractiveConstant,
                _idealLenght, _delta, _forceToStop);
            forceReachedMinimum = SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>.ComputeOneStep(_graphWithPosition);

            if (forceReachedMinimum) break;
            t++;
            Repaint();
            yield return null;
        }
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
        DrawNonZoomArea();

        // The zoom area clipping is sometimes not fully confined to the passed in rectangle. At certain
        // zoom levels you will get a line of pixels rendered outside of the passed in area because of
        // floating point imprecision in the scaling. Therefore, it is recommended to draw the zoom
        // area first and then draw everything else so that there is no undesired overlap.
        DrawZoomArea();


        

    }

    private void DrawGraph(AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> graphWithPosition)
    {
        // draw the graph
        foreach (VerticeWithPosition<RecipeStep> stepWithPosition in graphWithPosition.Vertices)
        {
            Color color = stepWithPosition.vertice.IsTerminal ? Color.red : stepWithPosition.vertice.IsInitialState ? Color.green : Color.gray;
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);
            Handles.color = Color.black;
            Handles.DrawWireDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = (int) Mathf.Clamp(12f/_zoom, 4f, 25f);

            EditorGUI.LabelField(new Rect(stepWithPosition.position.x, stepWithPosition.position.y, 200, 20), stepWithPosition.vertice.Name, style);
        }
        Handles.color = Color.white;


        foreach (TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink> edge in graphWithPosition.Edges)
        {
            Handles.DrawAAPolyLine(3, edge.Source.position, edge.Target.position);

            DrawArrowhead(edge.Source.position, edge.Target.position, 20, 8f);
        }
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

