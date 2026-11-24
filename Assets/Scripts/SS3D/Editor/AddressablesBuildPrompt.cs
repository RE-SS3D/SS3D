#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Stops the one-time "Addressables Build Report" dialog from blocking a player build.
    /// <para>
    /// Addressables asks once per Library whether to turn on "Debug Build Layout". The answer lives in
    /// <c>Library/AddressablesConfig.dat</c>, so a fresh Library (CI runners, new clones) shows the
    /// dialog on its first content build. Batchmode skips it, but the weekly graphical run builds from
    /// an open editor and stalls on it. The flag is internal to the package, hence the reflection.
    /// </para>
    /// </summary>
    internal static class AddressablesBuildPrompt
    {
        private const string TypeName = "UnityEditor.AddressableAssets.Settings.ProjectConfigData, Unity.Addressables.Editor";
        private const string PropertyName = "UserHasBeenInformedAboutBuildReportSettingPreBuild";

        public static void MarkAnswered()
        {
            PropertyInfo property = Type.GetType(TypeName)?.GetProperty(
                PropertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || !property.CanWrite)
            {
                Debug.LogWarning($"[Build] ProjectConfigData.{PropertyName} not found; the Addressables build report prompt may show.");
                return;
            }

            property.SetValue(null, true);
        }
    }
}
#endif
