#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(InteractionIconsAssetDatabase))]
    public class InteractionIconsAssetDatabaseInspectorEditor : AssetDatabaseInspectorEditor { }
}
#endif