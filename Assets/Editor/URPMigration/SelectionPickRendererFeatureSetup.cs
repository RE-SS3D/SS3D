#if UNITY_EDITOR
using SS3D.Rendering.URP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SS3D.Editor.URPMigration
{
    static class SelectionPickRendererFeatureSetup
    {
        const string RendererPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";
        const string SelectionShaderPath = "Assets/Art/Graphics/SelectionShader.shader";

        [InitializeOnLoadMethod]
        static void AutoWireOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                WireIfNeeded(silent: true);
            };
        }

        public static void WireFromMenu()
        {
            if (WireIfNeeded(silent: false))
                Debug.Log("Selection pick renderer feature wired on SS3D_ForwardPlusRenderer.");
        }

        public static void EnsureWired() => WireIfNeeded(silent: true);

        static bool WireIfNeeded(bool silent)
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                if (!silent) Debug.LogError($"Could not load renderer at {RendererPath}");
                return false;
            }

            bool changed = RemoveBrokenFeatures(renderer);
            SelectionPickRendererFeature pick = null;
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature is SelectionPickRendererFeature existing)
                {
                    pick = existing;
                    break;
                }
            }

            if (pick == null)
            {
                pick = ScriptableObject.CreateInstance<SelectionPickRendererFeature>();
                pick.name = "SelectionPickRendererFeature";
                AssetDatabase.AddObjectToAsset(pick, renderer);
                renderer.rendererFeatures.Add(pick);
                changed = true;
            }

            changed |= AssignShader(pick);
            changed |= SynchronizeFeatureMap(renderer);
            if (!changed) return false;

            EditorUtility.SetDirty(renderer);
            // Do not flush unrelated dynamic font atlases or runtime catalog caches on reload.
            AssetDatabase.SaveAssetIfDirty(renderer);
            return true;
        }

        static bool RemoveBrokenFeatures(UniversalRendererData renderer)
        {
            bool changed = false;
            for (int i = renderer.rendererFeatures.Count - 1; i >= 0; i--)
            {
                if (renderer.rendererFeatures[i] != null) continue;
                renderer.rendererFeatures.RemoveAt(i);
                changed = true;
            }

            string path = AssetDatabase.GetAssetPath(renderer);
            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                // A valid selection feature must survive reloads, retaining its local file ID.
                if (subAsset == null || subAsset == renderer || subAsset is SelectionPickRendererFeature)
                    continue;
                if (subAsset.name != "SelectionPickRendererFeature") continue;
                Object.DestroyImmediate(subAsset, true);
                changed = true;
            }
            return changed;
        }

        static bool AssignShader(SelectionPickRendererFeature feature)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(SelectionShaderPath) ?? Shader.Find("Custom/Selection");
            SerializedObject serializedFeature = new(feature);
            SerializedProperty property = serializedFeature.FindProperty("_selectionShader");
            if (property.objectReferenceValue == shader) return false;
            property.objectReferenceValue = shader;
            return serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool SynchronizeFeatureMap(UniversalRendererData renderer)
        {
            SerializedObject serializedRenderer = new(renderer);
            SerializedProperty map = serializedRenderer.FindProperty("m_RendererFeatureMap");
            if (map == null) return false;
            bool changed = map.arraySize != renderer.rendererFeatures.Count;
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out long id))
                    continue;
                SerializedProperty entry = map.GetArrayElementAtIndex(i);
                if (entry.longValue == id) continue;
                entry.longValue = id;
                changed = true;
            }
            if (changed) serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }
    }
}
#endif
