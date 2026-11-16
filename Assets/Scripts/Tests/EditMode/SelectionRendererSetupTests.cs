using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Rendering.URP;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    public class SelectionRendererSetupTests
    {
        [Test]
        public void RepeatedSetupPreservesSelectionFeatureAndItsRecoveryMap()
        {
            Type setup = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("SS3D.Editor.URPMigration.SelectionPickRendererFeatureSetup"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(setup);
            MethodInfo wire = setup.GetMethod("EnsureWired", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(wire);
            wire.Invoke(null, null);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset");
            SelectionPickRendererFeature original = renderer.rendererFeatures.OfType<SelectionPickRendererFeature>().Single();
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string _, out long originalId));

            wire.Invoke(null, null);
            Assert.AreSame(original, renderer.rendererFeatures.OfType<SelectionPickRendererFeature>().Single(),
                "A script reload must not destroy and recreate a healthy selection feature.");
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string _, out long currentId));
            Assert.AreEqual(originalId, currentId);
            SerializedObject serialized = new(renderer);
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            Assert.AreEqual(renderer.rendererFeatures.Count, map.arraySize);
            for (int i = 0; i < map.arraySize; i++)
            {
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out long id));
                Assert.AreEqual(id, map.GetArrayElementAtIndex(i).longValue, "Feature recovery map must match the active list.");
            }
        }
    }
}
