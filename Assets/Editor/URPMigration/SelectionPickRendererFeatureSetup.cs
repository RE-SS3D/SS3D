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
                {
                    return;
                }

                WireIfNeeded(silent: true);
            };
        }

        [MenuItem("SS3D/URP Migration/Wire Selection Pick Renderer Feature")]
        public static void WireFromMenu()
        {
            if (WireIfNeeded(silent: false))
            {
                Debug.Log("Selection pick renderer feature wired on SS3D_ForwardPlusRenderer.");
            }
        }

        public static void EnsureWired()
        {
            WireIfNeeded(silent: true);
        }

        static bool WireIfNeeded(bool silent)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                if (!silent)
                {
                    Debug.LogError($"Could not load renderer at {RendererPath}");
                }

                return false;
            }

            RemoveBrokenFeatures(renderer);

            foreach (var feature in renderer.rendererFeatures)
            {
                if (feature is SelectionPickRendererFeature existing)
                {
                    AssignShader(existing);
                    EditorUtility.SetDirty(renderer);
                    AssetDatabase.SaveAssets();
                    return false;
                }
            }

            var pickFeature = ScriptableObject.CreateInstance<SelectionPickRendererFeature>();
            pickFeature.name = "SelectionPickRendererFeature";
            AssignShader(pickFeature);

            AssetDatabase.AddObjectToAsset(pickFeature, renderer);
            renderer.rendererFeatures.Add(pickFeature);

            EditorUtility.SetDirty(pickFeature);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RendererPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        static void RemoveBrokenFeatures(UniversalRendererData renderer)
        {
            for (var i = renderer.rendererFeatures.Count - 1; i >= 0; i--)
            {
                var feature = renderer.rendererFeatures[i];
                if (feature == null || feature is SelectionPickRendererFeature)
                {
                    renderer.rendererFeatures.RemoveAt(i);
                }
            }

            var path = AssetDatabase.GetAssetPath(renderer);
            foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (subAsset == null || subAsset == renderer)
                {
                    continue;
                }

                if (subAsset is SelectionPickRendererFeature)
                {
                    Object.DestroyImmediate(subAsset, true);
                }
                else if (subAsset.name == "SelectionPickRendererFeature")
                {
                    Object.DestroyImmediate(subAsset, true);
                }
            }
        }

        static void AssignShader(SelectionPickRendererFeature feature)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SelectionShaderPath);
            if (shader == null)
            {
                shader = Shader.Find("Custom/Selection");
            }

            var serializedFeature = new SerializedObject(feature);
            serializedFeature.FindProperty("_selectionShader").objectReferenceValue = shader;
            serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
