using NUnit.Framework;
using SS3D.Systems.Tile.FloorVisuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorTests
{
    /// <summary>Floor stripes and decals render on the station shader, cut out by their texture alpha.</summary>
    public class FloorVisualMeshTests
    {
        [Test]
        public void CutoutMaterial_UsesStationShaderWithAlphaClip()
        {
            Texture2D texture = new(4, 4);
            Material material = FloorVisualMesh.CreateCutoutMaterial(texture, Color.red);
            try
            {
                Assert.That(material.shader.name, Is.EqualTo("SS3D/Station"));
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True);
                Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.AlphaTest));
                Assert.That(material.mainTexture, Is.SameAs(texture));
                Assert.That(material.color, Is.EqualTo(Color.red));
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
