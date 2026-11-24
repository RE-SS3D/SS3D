using System;
using System.Linq;
using Coimbra;
using SS3D.Application;
using SS3D.Networking.Settings;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Keeps a look-dev round's in-memory settings off disk. Coimbra marks every <see cref="ScriptableSettings"/> dirty
    /// on each domain reload (OnValidate), and entering Play reloads it here, so during a round any asset save would
    /// write the look-dev <c>TestScriptPath</c> and Host network type into the settings assets. While a round is active
    /// those two assets are left out of every save; they stay dirty and save normally once Play exits and
    /// <see cref="StationLookDev"/> has put their values back.
    /// </summary>
    public sealed class StationLookDevSaveGuard : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!StationLookDev.IsActive)
            {
                return paths;
            }

            string[] guarded =
            {
                AssetDatabase.GetAssetPath(ScriptableSettings.GetOrFind<ApplicationSettings>()),
                AssetDatabase.GetAssetPath(ScriptableSettings.GetOrFind<NetworkSettings>()),
            };

            string[] kept = paths.Where(path => !guarded.Contains(path, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (kept.Length != paths.Length)
            {
                Debug.Log("[look-dev] Left the application and network settings out of this save; they hold the "
                    + "round's in-memory values until Play exits.");
            }

            return kept;
        }
    }
}
