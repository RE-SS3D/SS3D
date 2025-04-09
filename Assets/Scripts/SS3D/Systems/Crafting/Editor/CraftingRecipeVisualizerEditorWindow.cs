using QuikGraph;
using SS3D.Utils;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Custom window to display recipe graph. It's node positions are chosen using a force constrained algorithm.
    /// This window should show the graph position being dynamically chosen, like an animation.
    /// </summary>
    public class CraftingRecipeVisualizerEditorWindow : EditorWindow
    {
        /// <summary>
        /// How much vertices are repulsive to each other.
        /// </summary>
        private const float RepulsiveConstant = 100;

        /// <summary>
        /// How much vertices linked by an edge attract each other.
        /// </summary>
        private const float AttractiveConstant = 3;

        /// <summary>
        /// Ideal lenght between vertices.
        /// </summary>
        private const float IdealLenght = 80;

        /// <summary>
        /// Maximum of interation the force algorithm will make.
        /// </summary>
        private const int MaxIteration = 500;

        /// <summary>
        /// "Speed" factor for the algorithm, the higher it is, the faster it converges
        /// toward the solution, but values too high can lead to divergence.
        /// </summary>
        private const float Delta = 10f;

        /// <summary>
        /// Another criteria to stop the algorithm is what's the max force exerted on any vertices is at a given iteration.
        /// When lower than a given amount we consider it won't move much, and we stop.
        /// </summary>
        private const float ForceToStop = 0.1f;

        /// <summary>
        /// Minimum zoom allowed
        /// </summary>
        private const float KZoomMin = 0.1f;

        /// <summary>
        /// Maximum zoom allowed
        /// </summary>
        private const float KZoomMax = 10.0f;

        /// <summary>
        /// Size of vertices drawn in the window.
        /// </summary>
        private const float CircleSize = 5f;

        /// <summary>
        /// Area in which the zooming will occur.
        /// </summary>
        private readonly Rect _zoomArea = new(0.0f, 100.0f, 1200.0f, 600.0f);

        /// <summary>
        /// Enhanced recipe Graph with position for vertices.
        /// </summary>
        private AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> _graphWithPosition;

        /// <summary>
        /// The current value of the zoom.
        /// </summary>
        private float _zoom = 1.0f;

        private Vector2 _zoomCoordsOrigin = Vector2.zero;

        [SerializeField]
        private AssetReferenceT<CraftingRecipe> _recipe;

        /// <summary>
        /// Show this window with the right size and parameters.
        /// </summary>
        [MenuItem("Window/SS3D/Crafting Recipe Display")]
        public static void ShowWindow()
        {
            CraftingRecipeVisualizerEditorWindow window = GetWindow<CraftingRecipeVisualizerEditorWindow>("Crafting Recipe Display");
            window.minSize = new(600.0f, 300.0f);
            window.wantsMouseMove = true;
        }

        protected void OnGUI()
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
        /// Helper method to find coordinates when zooming from the screen coordinates.
        /// </summary>
        private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return ((screenCoords - _zoomArea.TopLeft()) / _zoom) + _zoomCoordsOrigin;
        }

        /// <summary>
        /// Draw the area of the window that can be zoomed/panned.
        /// </summary>
        private void DrawZoomArea()
        {
            // Within the zoom area all coordinates are relative to the top left corner of the zoom area
            // with the width and height being scaled versions of the original/unzoomed area's width and height.
            EditorZoomArea.Begin(_zoom, _zoomArea);
            GUILayout.BeginArea(new(-_zoomCoordsOrigin.x, -_zoomCoordsOrigin.y, 1600.0f, 900.0f));

            if (_graphWithPosition != null)
            {
                DrawGraph(_graphWithPosition);
            }

            GUILayout.EndArea();
            EditorZoomArea.End();
        }

        /// <summary>
        /// Draw the area of the window that won't be zoomed/panned.
        /// </summary>
        private void DrawNonZoomArea()
        {
            EditorGUILayout.LabelField("Recipe display.", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scroll to zoom, drag to move.");

            if (GUILayout.Button("Draw graph"))
            {
                if (_recipe == null)
                {
                    return;
                }

                EditorCoroutineUtility.StartCoroutine(ComputeGraphPositions(_recipe.editorAsset), this);
            }

            SerializedObject so = new(this);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(_recipe)), new GUIContent("Recipe"));
        }

        /// <summary>
        /// Apply the spring embedder algorithm and do one step each frame, this is supposed to be called by a coroutine.
        /// </summary>
        private IEnumerator ComputeGraphPositions(CraftingRecipe recipe)
        {
            _graphWithPosition = SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>
                .InitializeGraphWithPositions(recipe.RecipeGraph);

            for (int i = 0; i < MaxIteration; i++)
            {
                SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>
                    .SetParameters(RepulsiveConstant, AttractiveConstant, IdealLenght, Delta, ForceToStop);

                bool forceReachedMinimum = SpringEmbedderAlgorithm<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>, RecipeStepLink>
                    .ComputeOneStep(_graphWithPosition);

                if (forceReachedMinimum)
                {
                    break;
                }

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
                float zoomDelta = -delta.y / 50.0f;
                float oldZoom = _zoom;
                _zoom += zoomDelta;
                _zoom = Mathf.Clamp(_zoom, KZoomMin, KZoomMax);
                _zoomCoordsOrigin += zoomCoordsMousePos - _zoomCoordsOrigin - ((oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin));

                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                Vector2 delta = Event.current.delta;
                delta /= _zoom;
                _zoomCoordsOrigin -= delta;
                Event.current.Use();
            }
        }

        /// <summary>
        /// Draw the graph
        /// </summary>
        private void DrawGraph(AdjacencyGraph<VerticeWithPosition<RecipeStep>, TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink>> graphWithPosition)
        {
            foreach (VerticeWithPosition<RecipeStep> stepWithPosition in graphWithPosition.Vertices)
            {
                Color color;

                if (stepWithPosition.Vertice.IsTerminal)
                {
                    color = Color.red;
                }
                else if (stepWithPosition.Vertice.IsInitialState)
                {
                    color = Color.green;
                }
                else
                {
                    color = Color.gray;
                }

                Handles.color = color;
                Handles.DrawSolidDisc(new(stepWithPosition.Position.x, stepWithPosition.Position.y, 0), Vector3.forward, CircleSize);
                Handles.color = Color.black;
                Handles.DrawWireDisc(new(stepWithPosition.Position.x, stepWithPosition.Position.y, 0), Vector3.forward, CircleSize);

                GUIStyle style = new(GUI.skin.label)
                {
                    fontSize = (int)Mathf.Clamp(12f / _zoom, 4f, 25f),
                };

                EditorGUI.LabelField(new(stepWithPosition.Position.x, stepWithPosition.Position.y, 200, 20), stepWithPosition.Vertice.Name, style);
            }

            Handles.color = Color.white;

            foreach (TaggedEdge<VerticeWithPosition<RecipeStep>, RecipeStepLink> edge in graphWithPosition.Edges)
            {
                Handles.DrawAAPolyLine(3, edge.Source.Position, edge.Target.Position);
                DrawArrowhead(edge.Source.Position, edge.Target.Position, 20, 8f);
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
