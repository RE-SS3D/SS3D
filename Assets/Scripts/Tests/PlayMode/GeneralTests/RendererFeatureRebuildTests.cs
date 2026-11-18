using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Rendering.URP;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a renderer rebuild during Play (a quality change, toggling or editing a renderer feature) keeps each
    // feature's materials alive; a dead material silently stops hover/examine (selection pick) and gas visuals
    // (atmos) · Not cheaper because: the fault needs Play Mode's deferred Object.Destroy, which Edit Mode replaces
    // with DestroyImmediate
    public class RendererFeatureRebuildTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator SelectionPick_KeepsItsMaterialAcrossARebuild() =>
            KeepsMaterialsAcrossARebuild<SelectionPickRendererFeature>();

        [UnityTest]
        public IEnumerator Atmos_KeepsItsMaterialsAcrossARebuild() =>
            KeepsMaterialsAcrossARebuild<AtmosRendererFeature>();

        private static IEnumerator KeepsMaterialsAcrossARebuild<T>()
            where T : ScriptableRendererFeature
        {
            T feature = ScriptableObject.CreateInstance<T>();
            try
            {
                feature.Create();

                // URP's rebuild order: the old renderer disposes the shared feature, then the new renderer
                // creates it, in the same frame.
                feature.Dispose();
                feature.Create();

                // Object.Destroy lands at the end of the frame.
                yield return null;

                FieldInfo[] materials = typeof(T).GetFields(Private)
                    .Where(field => field.FieldType == typeof(Material))
                    .ToArray();
                Assert.That(materials, Is.Not.Empty, $"{typeof(T).Name} has material fields");
                foreach (FieldInfo field in materials)
                {
                    Assert.That((Material)field.GetValue(feature) != null, Is.True,
                        $"{typeof(T).Name}.{field.Name} is alive after the rebuild");
                }
            }
            finally
            {
                feature.Dispose();
                Object.Destroy(feature);
            }
        }
    }
}
