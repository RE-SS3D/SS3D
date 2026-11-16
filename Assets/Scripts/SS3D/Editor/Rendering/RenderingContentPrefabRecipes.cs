#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Rendering
{
    /// <summary>
    /// Domain recipe aggregator for rendering content hygiene (tier B — see
    /// 2026-07_editor-tooling-tiers.md).
    /// </summary>
    public static class RenderingContentPrefabRecipes
    {
        [MenuItem("SS3D/Rendering/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            int probePrefabs = RendererProbeUsageSetup.SetupAll();
            EditorUtility.DisplayDialog(
                "Rendering Content Recipes",
                $"Disabled light/reflection probes on renderers in {probePrefabs} prefab(s) under Assets/Content.\n" +
                "(SS3D does not use probe lighting; BlendProbes blocks GPU instancing.)",
                "OK");
        }

        /// <summary>
        /// BatchMode: <c>-executeMethod SS3D.Editor.Rendering.RenderingContentPrefabRecipes.RunAllBatch</c>
        /// </summary>
        public static void RunAllBatch()
        {
            int probePrefabs = RendererProbeUsageSetup.SetupAll();
            Debug.Log($"[RenderingContentPrefabRecipes] Probe usage cleared on {probePrefabs} prefab(s).");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif
