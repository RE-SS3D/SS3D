#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Editor.Rendering
{
    /// <summary>
    /// Idempotent content hygiene: SS3D does not use light probes or reflection probes for
    /// station/entity shading. Leaving BlendProbes on MeshRenderers feeds Frame Debugger
    /// "Non-instanced properties set for instanced shader" and blocks GPU instancing on
    /// Simple Toon / Palette draws. Tier B — see 2026-07_editor-tooling-tiers.md.
    /// </summary>
    public static class RendererProbeUsageSetup
    {
        private const string ContentRoot = "Assets/Content";

        /// <summary>
        /// Walks every prefab under <see cref="ContentRoot"/> and sets
        /// <see cref="LightProbeUsage.Off"/> + <see cref="ReflectionProbeUsage.Off"/> on all
        /// <see cref="Renderer"/>s (Mesh + Skinned). Returns the number of prefabs written.
        /// </summary>
        public static int SetupAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ContentRoot });
            int prefabsTouched = 0;
            int renderersTouched = 0;
            var dirtyPaths = new List<string>(64);

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    float progress = (float)i / Mathf.Max(1, guids.Length);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Renderer Probe Usage",
                            path,
                            progress))
                    {
                        break;
                    }

                    if (TrySetupPrefab(path, out int rendererCount))
                    {
                        prefabsTouched++;
                        renderersTouched += rendererCount;
                        dirtyPaths.Add(path);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (dirtyPaths.Count > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                $"[RendererProbeUsageSetup] Updated {renderersTouched} renderer(s) across {prefabsTouched} prefab(s) under {ContentRoot}.");
            return prefabsTouched;
        }

        private static bool TrySetupPrefab(string path, out int renderersChanged)
        {
            renderersChanged = 0;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[RendererProbeUsageSetup] Missing {path}");
                return false;
            }

            bool dirty = false;
            try
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    // Nested prefab instance renderers: record as overrides on this outer prefab
                    // when needed; source nested prefabs are also walked as their own assets.
                    if (renderer.lightProbeUsage != LightProbeUsage.Off)
                    {
                        renderer.lightProbeUsage = LightProbeUsage.Off;
                        dirty = true;
                        renderersChanged++;
                    }

                    if (renderer.reflectionProbeUsage != ReflectionProbeUsage.Off)
                    {
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        dirty = true;
                        renderersChanged++;
                    }
                }

                if (dirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return dirty;
        }
    }
}
#endif
