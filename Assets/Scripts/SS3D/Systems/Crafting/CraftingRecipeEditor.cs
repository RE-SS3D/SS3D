using UnityEditor;
using UnityEngine;
using SS3D.Systems.Crafting;
using System.Collections.Generic;
using QuikGraph;
using System.Linq;

public class CraftingRecipeEditor : EditorWindow
{
    private CraftingRecipe _recipe;

    private struct RecipeStepWithPosition
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
        // Load or create your GraphData here
        //graphData = ScriptableObject.CreateInstance<GraphData>();

        // Try to get the GraphData from the selected object in the Inspector
        _recipe = Selection.activeObject as CraftingRecipe;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Recipe display", EditorStyles.boldLabel);

        AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graphWithPosition = new();

        // make a new graph giving positions to edges.
        foreach(var edge in _recipe.RecipeGraph.Edges)
        {
            RecipeStepWithPosition source = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Source);
            RecipeStepWithPosition target = graphWithPosition.Vertices.FirstOrDefault(x => x.step == edge.Target);

            var edgeWithPosition = new TaggedEdge<RecipeStepWithPosition, RecipeStepLink>(
                source.step != null ? source : new RecipeStepWithPosition() { step = edge.Source, position = GetRandomCirclePosition() },
                target.step != null ? target : new RecipeStepWithPosition() { step = edge.Target, position = GetRandomCirclePosition() },
                edge.Tag
                );
            graphWithPosition.AddVerticesAndEdge(edgeWithPosition);
        }

        // draw the graph
        foreach (var stepWithPosition in graphWithPosition.Vertices)
        {
            Handles.DrawWireDisc(new Vector3(stepWithPosition.position.x, stepWithPosition.position.y, 0), Vector3.forward, 10f);
        }

        foreach(TaggedEdge<RecipeStepWithPosition, RecipeStepLink> edge in graphWithPosition.Edges)
        {
            Handles.DrawLine(edge.Source.position, edge.Target.position);
        }
    }

    private Vector2 GetRandomCirclePosition()
    {
        return Random.insideUnitCircle * 300 + new Vector2(400, 400);
    }

    private List<RecipeStepWithPosition> SpringEmbedderAlgorithm(AdjacencyGraph<RecipeStepWithPosition, TaggedEdge<RecipeStepWithPosition, RecipeStepLink>> graph)
    {
        //graph.TryGetEdge
        return null;
    }
}
