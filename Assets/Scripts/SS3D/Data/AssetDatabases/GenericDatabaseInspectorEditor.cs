#if UNITY_EDITOR
using SS3D.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    [CustomEditor(typeof(InteractionIconsAssetDatabase))]
    public class GenericDatabaseInspectorEditor : Editor
    {
        private GenericAssetDatabase _assetDatabase;

        private void OnEnable()
        {
            _assetDatabase = (InteractionIconsAssetDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);

            if (GUILayout.Button($"Create Enum", GUILayout.Width(500)))
            {
                EnumCreator.CreateAtAssetPath(_assetDatabase, _assetDatabase.EnumName, _assetDatabase.Assets);
            }

        }
    }
}
#endif