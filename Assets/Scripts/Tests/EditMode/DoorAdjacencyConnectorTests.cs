using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace EditorTests
{
    public class DoorAdjacencyConnectorTests
    {
        [Test]
        public void IsNorthSouthReturnsTrueOnlyForNorthAndSouth()
        {
            Assert.IsTrue(DoorAdjacencyConnector.IsNorthSouth(Direction.North));
            Assert.IsTrue(DoorAdjacencyConnector.IsNorthSouth(Direction.South));
            Assert.IsFalse(DoorAdjacencyConnector.IsNorthSouth(Direction.East));
            Assert.IsFalse(DoorAdjacencyConnector.IsNorthSouth(Direction.West));
        }

        [Test]
        public void SetRendererMaterialOnlyUpdatesRequestedSlot()
        {
            GameObject gameObject = new("test renderer");
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            Material firstMaterial = new(Shader.Find("Standard"));
            Material secondMaterial = new(Shader.Find("Standard"));
            Material replacementMaterial = new(Shader.Find("Standard"));
            renderer.sharedMaterials = new[] { firstMaterial, secondMaterial };

            DoorAdjacencyConnector.SetRendererMaterial(renderer, 1, replacementMaterial);

            Assert.AreSame(firstMaterial, renderer.sharedMaterials[0]);
            Assert.AreSame(replacementMaterial, renderer.sharedMaterials[1]);

            Object.DestroyImmediate(firstMaterial);
            Object.DestroyImmediate(secondMaterial);
            Object.DestroyImmediate(replacementMaterial);
            Object.DestroyImmediate(gameObject);
        }
    }
}
