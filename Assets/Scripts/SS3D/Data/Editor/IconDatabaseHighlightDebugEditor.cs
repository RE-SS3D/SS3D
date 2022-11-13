#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Data
{
    [CustomEditor(typeof(IconDatabase))]
    public class IconDatabaseHighlightDebugEditor : Editor
    {
        private IconDatabase _database;

        private void OnEnable()
        {
            _database = (IconDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);

            if (GUILayout.Button($"Create Enum", GUILayout.Width(500)))
            {
                _database.CreateEnum();
            }
        }
    }
}
#endif