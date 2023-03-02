#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Attributes
{       
    /// <summary>
    /// Attribute to make a property not editable on the inspector.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

    /// <summary>
    /// Creates a read only drawer on a serialized property.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif