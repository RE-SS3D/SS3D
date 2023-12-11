#if UNITY_EDITOR

using Serilog.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SS3D.Logging.LogSettings;

namespace SS3D.Logging.LogSettings.InspectorEditor
{
    /// <summary>
    /// Custom inspector for the log settings, allow to show the list of namespaces in a convenient manner,
    /// as well as resetting easily all namespaces logging level.
    /// </summary>
    [CustomEditor(typeof(LogSettings))]
    public class LogSettingsInspectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var spDefaultLevel = serializedObject.FindProperty("defaultLogLevel");

            // default log level, the log level at which all namespace will be by default.
            LogEventLevel defaultLevel = (LogEventLevel)EditorGUILayout.EnumPopup(new GUIContent("Default log level"), (LogEventLevel)spDefaultLevel.enumValueIndex);

            spDefaultLevel.enumValueIndex = (int)defaultLevel;

            var sp = serializedObject.FindProperty("SS3DNameSpaces");

            // Button to reset all namespaces to the default log level.
            if (GUILayout.Button("Reset to default log level"))
            {
                for (int i = 0; i < sp.arraySize; i++)
                {
                    sp.GetArrayElementAtIndex(i).FindPropertyRelative("_level").enumValueIndex = (int)defaultLevel;
                }
            }

            // show all namespaces along the log level in the inspector.
            for (int i = 0; i < sp.arraySize; i++)
            {
                EditorGUILayout.PropertyField(sp.GetArrayElementAtIndex(i));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif