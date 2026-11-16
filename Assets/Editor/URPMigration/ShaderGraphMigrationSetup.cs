#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.URPMigration
{
    static class ShaderGraphMigrationSetup
    {
        static readonly string[] ShaderGraphPaths =
        {
            "Assets/Content/WorldObjects/World/VFX/HologramShader.shadergraph",
            "Assets/Content/WorldObjects/World/VFX/DissolveEffect.shadergraph",
        };

        const string BuiltInLitSubTarget = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitSubTarget";
        const string UniversalLitSubTarget = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalLitSubTarget";
        const string BuiltInTarget = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInTarget";
        const string UniversalTarget = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget";

        const string UniversalTargetExtras =
            ",\n    \"m_CastShadows\": true,\n    \"m_ReceiveShadows\": true,\n    \"m_DisableTint\": false,\n    \"m_Sort3DAs2DCompatible\": false,\n    \"m_AdditionalMotionVectorMode\": 0,\n    \"m_AlembicMotionVectors\": false,\n    \"m_SupportsLODCrossFade\": false,\n    \"m_SupportVFX\": false";

        [InitializeOnLoadMethod]
        static void AutoConvertOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (!NeedsConversion())
                {
                    return;
                }

                if (Run(silent: true))
                {
                    Debug.Log("VFX shader graph URP conversion completed automatically.");
                }
            };
        }

        public static void ConvertFromMenu()
        {
            if (Run(silent: false))
            {
                Debug.Log("VFX shader graph URP conversion completed.");
            }
        }

        public static bool Run(bool silent = false)
        {
            var convertedAny = false;

            foreach (var path in ShaderGraphPaths)
            {
                if (!File.Exists(path))
                {
                    if (!silent)
                    {
                        Debug.LogWarning($"Shader graph not found at {path}");
                    }

                    continue;
                }

                if (ConvertShaderGraphFile(path))
                {
                    convertedAny = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            if (convertedAny)
            {
                AssetDatabase.SaveAssets();
            }

            return convertedAny;
        }

        static bool NeedsConversion()
        {
            foreach (var path in ShaderGraphPaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                if (File.ReadAllText(path).Contains(BuiltInTarget))
                {
                    return true;
                }
            }

            return false;
        }

        static bool ConvertShaderGraphFile(string path)
        {
            var content = File.ReadAllText(path);
            if (!content.Contains(BuiltInTarget))
            {
                return false;
            }

            content = content.Replace(BuiltInLitSubTarget, UniversalLitSubTarget);
            content = content.Replace(BuiltInTarget, UniversalTarget);
            content = Regex.Replace(
                content,
                @"""m_CustomEditorGUI"": """"\s*\n\}",
                "\"m_CustomEditorGUI\": \"\"" + UniversalTargetExtras + "\n}");

            File.WriteAllText(path, content);
            return true;
        }
    }
}
#endif
