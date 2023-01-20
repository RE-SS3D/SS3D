#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(IDPermissionsAssetDatabase))]
    public class IDPermissionsAssetDatabaseInspectorEditor : GenericAssetDatabaseInspectorEditor { }
}
#endif