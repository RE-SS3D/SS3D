using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    public class ForwardPlusLightingTests
    {
        private const string RendererPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";

        private static readonly Regex AdditionalLightsPragma =
            new(@"#pragma\s+multi_compile\w*\s[^\n]*\b_ADDITIONAL_LIGHTS\b", RegexOptions.Compiled);

        private static readonly Regex ClusterLoopPragma =
            new(@"#pragma\s+multi_compile\w*\s[^\n]*\b_CLUSTER_LIGHT_LOOP\b", RegexOptions.Compiled);

        // Forward shades each mesh with only its 8 most important lights, picked without regard to which side
        // they light. Fixtures put 13-27 lights in range of every tile, so on Forward neighbouring wall meshes
        // keep different lights: shadows are cut off at wall seams, and on low limits walls render black.
        [Test]
        public void RendererIsForwardPlus()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);

            Assert.That(renderer.renderingMode, Is.EqualTo(RenderingMode.ForwardPlus));
        }

        // On Forward+ URP enables _CLUSTER_LIGHT_LOOP instead of per-object lights, and GetAdditionalLightsCount()
        // returns 0. A shader without the cluster variant renders with no fixture light and no error.
        [Test]
        public void EveryShaderWithAdditionalLightsCompilesTheClusterLightLoop()
        {
            string[] missing = Directory.GetFiles("Assets", "*.shader", SearchOption.AllDirectories)
                .Where(path =>
                {
                    string source = File.ReadAllText(path);
                    return AdditionalLightsPragma.IsMatch(source) && !ClusterLoopPragma.IsMatch(source);
                })
                .ToArray();

            Assert.That(missing, Is.Empty);
        }
    }
}
