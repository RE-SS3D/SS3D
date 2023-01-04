#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Data
{
    [CustomEditor(typeof(InteractionIconsDatabase))]
    public class LabelButtonHighlightDebugEditor : Editor
    {
        private InteractionIconsDatabase _database;

        private void OnEnable()
        {
            _database = (InteractionIconsDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);

            if (GUILayout.Button($"Create Enum", GUILayout.Width(500)))
            {
                AssetData.CreateEnum(_database, _database.EnumName, _database.Assets);
            }

        }
    }
}
#endif