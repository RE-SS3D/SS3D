using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Crafting
{
    [CustomEditor(typeof(CraftingRecipe))]
    public class CraftingRecipeEditor : Editor
    {
        private SerializedProperty targetProperty;
        private SerializedProperty stepsProperty;
        private SerializedProperty stepLinksProperty;

        private void OnEnable()
        {
            // Initialize SerializedProperties
            targetProperty = serializedObject.FindProperty("_target");
            stepsProperty = serializedObject.FindProperty("steps");
            stepLinksProperty = serializedObject.FindProperty("stepLinks");
        }

        public override void OnInspectorGUI()
        {
            // Update SerializedObject
            serializedObject.Update();

            // Draw target property
            EditorGUILayout.PropertyField(targetProperty);

            // Check if any step has _isInitial set to true
            bool hasInitialStep = HasInitialStep();

            // Draw each RecipeStep individually
            if (stepsProperty.isExpanded)
            {
                for (int i = 0; i < stepsProperty.arraySize; i++)
                {
                    SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(i);
                    DrawRecipeStep(stepProperty, hasInitialStep);

                    EditorGUILayout.Space();
                }
            }

            // Draw stepLinks list property
            EditorGUILayout.PropertyField(stepLinksProperty, true); // 'true' means to draw children

            // Apply changes to SerializedObject
            serializedObject.ApplyModifiedProperties();
        }

        // Custom drawer for RecipeStep
        private void DrawRecipeStep(SerializedProperty stepProperty, bool hasInitialStep)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isInitialStep = false;

            bool isTerminalStep = false;

            // Iterate over properties of RecipeStep
            foreach (SerializedProperty property in stepProperty)
            {

                if (property.name == "_isInitialState" && hasInitialStep && property.boolValue == false)
                    continue;

                if (property.name == "_isInitialState" && property.boolValue == true) isInitialStep = true;

                if (property.name == "_isTerminal" && property.boolValue == true) isTerminalStep = true;

                if (isInitialStep && property.name == "_isTerminal")
                    continue;

                if (!isTerminalStep && property.name == "_customCraft")
                    continue;

                if (!isTerminalStep && property.name == "_result")
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            EditorGUILayout.EndVertical();
        }

        private bool HasInitialStep()
        {
            if (stepsProperty.isExpanded)
            {
                for (int i = 0; i < stepsProperty.arraySize; i++)
                {
                    SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty isInitialProperty = stepProperty.FindPropertyRelative("_isInitialState");
                    if (isInitialProperty != null && isInitialProperty.boolValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

