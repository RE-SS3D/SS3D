using Coimbra;
using SS3D.Data.Enums;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Asset database for all the interaction icons
    /// </summary>
    [ProjectSettings("SS3D/Assets", "Interaction Icons")]
    public class InteractionIconsAssetDatabase : AssetDatabase { }
}