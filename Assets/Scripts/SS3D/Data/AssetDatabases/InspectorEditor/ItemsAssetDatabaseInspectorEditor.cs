#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(ItemsAssetDatabase))]
    public class ItemsAssetDatabaseInspectorEditor : GenericAssetDatabaseInspectorEditor { }
}
#endif