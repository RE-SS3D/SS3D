using NUnit.Framework;
using SS3D.Systems.Selection;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Skinned meshes are picked by drawing the renderer with its own property block. A per-material block (power
    /// emission on the external airlock's Palette material) replaces the renderer-wide one for its material, which
    /// hid the selection colour and left only the door's frame pickable.
    /// </summary>
    public sealed class SkinnedSelectionColorTests
    {
        private static readonly int SelectionColorId = Shader.PropertyToID("_SelectionColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Test]
        public void SelectionColour_ReachesPerMaterialBlocksAndKeepsTheirValues()
        {
            GameObject go = new("Skinned pick test");
            try
            {
                SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMaterials = new[] { new Material(Shader.Find("Universal Render Pipeline/Unlit")), new Material(Shader.Find("Universal Render Pipeline/Unlit")) };
                Color32 selection = new(12, 34, 56, 255);

                MaterialPropertyBlock block = new();
                block.SetColor(SelectionColorId, selection);
                renderer.SetPropertyBlock(block);
                MaterialPropertyBlock emission = new();
                emission.SetColor(EmissionColorId, Color.green);
                renderer.SetPropertyBlock(emission, 0);

                Selectable.SyncSkinnedSelectionColor(renderer, selection, 2);

                MaterialPropertyBlock read = new();
                renderer.GetPropertyBlock(read, 0);
                Assert.That(read.HasColor(SelectionColorId), Is.True, "the per-material block carries the pick colour");
                Assert.That(((Color32)read.GetColor(SelectionColorId)).ToString(), Is.EqualTo(selection.ToString()), "exactly, as picks match by Color32");
                Assert.That(read.GetColor(EmissionColorId), Is.EqualTo(Color.green), "and keeps the emission it had");
                renderer.GetPropertyBlock(read, 1);
                Assert.That(read.isEmpty, Is.True, "a material without its own block keeps reading the renderer-wide one");
            }
            finally
            {
                foreach (Material material in go.GetComponent<SkinnedMeshRenderer>().sharedMaterials)
                    Object.DestroyImmediate(material);
                Object.DestroyImmediate(go);
            }
        }
    }
}
