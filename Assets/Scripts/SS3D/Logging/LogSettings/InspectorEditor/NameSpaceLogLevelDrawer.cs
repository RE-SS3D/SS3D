#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SS3D.Logging.LogSettings.InspectorEditor
{
	[CustomPropertyDrawer(typeof(NamespaceLogLevel))]
	public class NameSpaceLogLevelDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Calculate rect
			Rect unitRect = new Rect(position.x + 35, position.y, 150, position.height);

			// Draw fields - pass GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("_level"), GUIContent.none);

			EditorGUI.EndProperty();
		}
	}
}
# endif