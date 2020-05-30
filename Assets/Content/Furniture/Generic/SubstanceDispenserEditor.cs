using System;
using SS3D.Engine.Substances;
using UnityEditor;

namespace SS3D.Content.Furniture.Generic
{
    [CustomEditor(typeof(SubstanceDispenser))]
    public class SubstanceDispenserEditor : Editor
    {
        private SerializedProperty nameProperty;
        private SerializedProperty moleProperty;
        private SubstanceRegistry registry;
        private Substance substance;

        private void OnEnable()
        {
            nameProperty = serializedObject.FindProperty("Substance");
            moleProperty = serializedObject.FindProperty("Moles");
            registry = FindObjectOfType<SubstanceRegistry>();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            float moles = moleProperty.floatValue;
            if (registry != null && (substance == null || substance.Id != nameProperty.stringValue))
            {
                substance = registry.FromId(nameProperty.stringValue);
            }
            
            EditorGUILayout.PropertyField(nameProperty);
            float newMoles = EditorGUILayout.FloatField("Moles", moles);
            if (Math.Abs(newMoles - moles) > 0.00001)
            {
                moleProperty.floatValue = newMoles;
                moles = newMoles;
            }

            if (substance != null)
            {
                newMoles = EditorGUILayout.FloatField("Volume (ml)", moles * substance.MillilitersPerMole) / substance.MillilitersPerMole;
                if (Math.Abs(newMoles - moles) > 0.00001)
                {
                    moleProperty.floatValue = newMoles;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}