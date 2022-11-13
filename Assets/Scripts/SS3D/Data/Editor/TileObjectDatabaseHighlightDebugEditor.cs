#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SS3D.Data
{
    [CustomEditor(typeof(TileObjectDatabase))]
    public class TileObjectDatabaseHighlightDebugEditor : Editor
    {
        private TileObjectDatabase _database;

        private void OnEnable()
        {
            _database = (TileObjectDatabase)target;
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