#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.URPMigration
{
    static class URPStragglerMigrationSetup
    {
        const string GenericShadelessPath = "Assets/Content/WorldObjects/World/Materials/GenericShadeless.mat";
        const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";

        [InitializeOnLoadMethod]
        static void AutoMigrateOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (!NeedsMigration())
                {
                    return;
                }

                if (Run(silent: true))
                {
                    Debug.Log("URP straggler migration completed automatically.");
                }
            };
        }

        public static void MigrateFromMenu()
        {
            if (Run(silent: false))
            {
                Debug.Log("URP straggler migration completed.");
            }
        }

        public static bool Run(bool silent = false)
        {
            if (!MigrateGenericShadeless(silent))
            {
                return false;
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        static bool NeedsMigration()
        {
            return UsesLegacyShader(GenericShadelessPath, "Unlit/Color", "Legacy Shaders/");
        }

        static bool MigrateGenericShadeless(bool silent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GenericShadelessPath);
            if (material == null || material.shader == null)
            {
                return false;
            }

            if (!material.shader.name.Contains("Unlit/Color") && material.shader.name != "Legacy Shaders/Unlit/Color")
            {
                return false;
            }

            var urpUnlit = Shader.Find(UrpUnlitShaderName);
            if (urpUnlit == null)
            {
                if (!silent)
                {
                    Debug.LogError($"Could not find shader '{UrpUnlitShaderName}'.");
                }

                return false;
            }

            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.black;
            material.shader = urpUnlit;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(material);
            if (!silent)
            {
                Debug.Log($"Converted {GenericShadelessPath} to {UrpUnlitShaderName}.");
            }

            return true;
        }

        static bool UsesLegacyShader(string materialPath, params string[] legacyShaderNameFragments)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.shader == null)
            {
                return false;
            }

            var shaderName = material.shader.name;
            foreach (var fragment in legacyShaderNameFragments)
            {
                if (shaderName.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
