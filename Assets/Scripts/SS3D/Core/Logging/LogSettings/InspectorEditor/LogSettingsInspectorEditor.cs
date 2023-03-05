#if UNITY_EDITOR

using Serilog.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Logging.LogSettings.InspectorEditor
{
    [CustomEditor(typeof(LogSetting))]
    public class LogSettingsInspectorEditor : Editor
    {
        private LogSetting _logSetting;

        private ScrollView _assetsListView;
        private Button _loadAssetsButton;
        private ObjectField _assetGroupObjectField;
        private Label _assetDatabaseLabel;
        private TextField _enumNameTextField;

        private void OnEnable()
        {
            _logSetting = (LogSetting)target;  
        }

        /// <summary>
        /// This sets ups the UI for the custom inspector using the UI Toolkit
        /// </summary>
        /// <returns></returns>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var spDefaultLevel = serializedObject.FindProperty("defaultLogLevel");

            LogEventLevel defaultLevel = (LogEventLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Default log level")
                , (LogEventLevel)spDefaultLevel.enumValueIndex );

            spDefaultLevel.enumValueIndex = (int)defaultLevel;

            var sp = serializedObject.FindProperty("SS3DNameSpaces");

            if (GUILayout.Button("Reset to default log level"))
            {
                for (int i = 0; i < sp.arraySize; i++)
                {
                    sp.GetArrayElementAtIndex(i).FindPropertyRelative("_level").enumValueIndex = (int)defaultLevel;
                }
                
            }

            for (int i = 0; i < sp.arraySize; i++)
            {
                EditorGUILayout.PropertyField(sp.GetArrayElementAtIndex(i));
            }
            
            serializedObject.ApplyModifiedProperties();

            
        }
    }
}
#endif