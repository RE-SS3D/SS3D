using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// One-shot repair for humanoid animator parameters that were hand-edited into YAML
    /// and fail <c>Animator.Set*</c> at runtime despite appearing in the controller asset.
    /// </summary>
    [InitializeOnLoad]
    internal static class HumanoidAnimatorParameterRepair
    {
        /// <summary>Bump to re-run the repair after future controller YAML hand-edits.</summary>
        private const string DoneKey = "SS3D.HumanoidAnimatorParameterRepair.v1";

        static HumanoidAnimatorParameterRepair()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += TryRunOnce;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryRunOnce;
            }
        }

        private static void TryRunOnce()
        {
            if (EditorPrefs.GetBool(DoneKey, false))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string result = HumanoidLocomotionBlendSetup.RebindHumanoidAnimatorParameters();
            Debug.Log($"[HumanoidAnimatorParameterRepair] {result}");
            if (!result.StartsWith("ERROR"))
            {
                EditorPrefs.SetBool(DoneKey, true);
            }
        }
    }
}
