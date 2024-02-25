using UnityEditor;
using UnityEngine;
using QuikGraph;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System;
using SS3D.Utils;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Custom window to display the recipe graph of a recipe opened in inspector. The 
    /// recipe graph is shown as a visual graph, and it's node positions are chosen using a force constrained algorithm.
    /// This window should show the graph position being dynamically chosen, like an animation.
    /// It need a craftin recipe open in inspector to show the graph.
    /// </summary>
    public class CraftingRecipeVisualizerEditorWindow : EditorWindow
    {
        /// <summary>
        /// How much vertices are repulsive to each other.
        /// </summary>
        private float _repulsiveConstant = 1;

        /// <summary>
        /// How much vertices linked by an an edge attract each other.
        /// </summary>
        private float _attractiveConstant = 1;

        /// <summary>
        /// Ideal lenght between vertices.
        /// </summary>
        private float _idealLenght = 80;

        /// <summary>
        /// How much iteration will the force algorithm make before stopping.
        /// </summary>
        private int _maxIteration = 300;

        /// <summary>
        /// Delta acts like a "speed" factor for the algorithm, the higher it is, the faster it converges
        /// toward the solution, but values too high can lead to divergence.
        /// </summary>
        private float _delta = 1f;

        /// <summary>
        /// Another criteria to stop the algorithm is what's the max force exerted on any vertices is at a given iteration.
        /// When lower than a given amount we consider it won't move much still, and we stop.
        /// </summary>
        private float _forceToStop = 0.1f;

        /// <summary>
        /// Enhanced recipe Graph with position for vertices. 
        /// </summary>
        private AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> _graphWithPosition;

        /// <summary>
        /// Minimum zoom allowed
        /// </summary>
        private const float KZoomMin = 0.1f;

        /// <summary>
        /// Maximum zoom allowed
        /// </summary>
        private const float KZoomMax = 10.0f;

        /// <summary>
        /// Area in which the zooming will occur.
        /// </summary>
        private readonly Rect _zoomArea = new Rect(200.0f, 200.0f, 1200.0f, 600.0f);

        /// <summary>
        /// The current value of the zoom.
        /// </summary>
        private float _zoom = 1.0f;

        private Vector2 _zoomCoordsOrigin = Vector2.zero;

        /// <summary>
        /// Size of vertices drawn in the window.
        /// </summary>
        private const float CircleSize = 5f;

        /// <summary>
        /// Helper method to find coordinates when zooming from the screen coordinates.
        /// </summary>
        private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return (screenCoords - _zoomArea.TopLeft()) / _zoom + _zoomCoordsOrigin;
        }

        /// <summary>
        /// Draw the area of the window that can be zoomed/panned.
        /// </summary>
        private void DrawZoomArea()
        {
            // Within the zoom area all coordinates are relative to the top left corner of the zoom area
            // with the width and height being scaled versions of the original/unzoomed area's width and height.
            EditorZoomArea.Begin(_zoom, _zoomArea);

            GUILayout.BeginArea(new Rect(-_zoomCoordsOrigin.x, -_zoomCoordsOrigin.y, 1600.0f, 900.0f));

            if (_graphWithPosition != null) DrawGraph(_graphWithPosition);

            GUILayout.EndArea();

            EditorZoomArea.End();
        }

        /// <summary>
        /// Draw the area of the window that won't be zoomed/panned.
        /// </summary>
        private void DrawNonZoomArea()
        {
            EditorGUILayout.LabelField("Recipe display", EditorStyles.boldLabel);

            if (GUILayout.Button("draw graph"))
            {
                CraftingRecipe recipe = UnityEditor.Selection.activeObject as CraftingRecipe;

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

        /// <summary>
        /// Apply the spring embedder algorithm and do one step each frame, this is supposed to be called by a coroutine.
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Handle user inputs for zoom and panning
        /// </summary>
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

        /// <summary>
        /// Show this window with the right size and parameters.
        /// </summary>
        [MenuItem("Window/SS3D/Crafting Recipe Display")]
        public static void ShowWindow()
        {
            CraftingRecipeVisualizerEditorWindow window = GetWindow<CraftingRecipeVisualizerEditorWindow>("Crafting Recipe Display");
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

        /// <summary>
        /// Draw the graph
        /// </summary>
        private void DrawGraph(AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> graphWithPosition)
        {
            foreach (VerticeWithPosition<RecipeStep> stepWithPosition in graphWithPosition.Vertices)
            {
                Color color = stepWithPosition.vertice.IsTerminal ? Color.red : stepWithPosition.vertice.IsInitialState ? Color.green : Color.gray;
                Handles.color = color;
                Handles.DrawSolidDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);
                Handles.color = Color.black;
                Handles.DrawWireDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, CircleSize);

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = (int)Mathf.Clamp(12f / _zoom, 4f, 25f);

                EditorGUI.LabelField(new Rect(stepWithPosition.position.x, stepWithPosition.position.y, 200, 20), stepWithPosition.vertice.Name, style);
            }
            Handles.color = Color.white;


            foreach (TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink> edge in graphWithPosition.Edges)
            {
                Handles.DrawAAPolyLine(3, edge.Source.position, edge.Target.position);

                DrawArrowhead(edge.Source.position, edge.Target.position, 20, 8f);
            }
        }

        /// <summary>
        /// Helper method to draw the arrowhead at the tip of edges.
        /// </summary>
        private void DrawArrowhead(Vector2 start, Vector2 end, float arrowheadAngle, float arrowheadLength)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 arrowheadLeft = Quaternion.Euler(0, 0, arrowheadAngle) * -direction * arrowheadLength;
            Vector2 arrowheadRight = Quaternion.Euler(0, 0, -arrowheadAngle) * -direction * arrowheadLength;

            Handles.DrawAAPolyLine(3, new Vector3(end.x, end.y, 0), new Vector3(end.x + arrowheadLeft.x, end.y + arrowheadLeft.y, 0));
            Handles.DrawAAPolyLine(3, new Vector3(end.x, end.y, 0), new Vector3(end.x + arrowheadRight.x, end.y + arrowheadRight.y, 0));
        }
    }
}

