using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;

namespace EditorTests
{
    /// <summary>Anything worn on the head hides the head hair; beards, eyebrows and other slots leave it alone.</summary>
    public class ClothHairOcclusionTests
    {
        private const string Clothing = "Assets/Content/WorldObjects/Items/Clothing/";

        private GameObject _head;
        private Renderer _hair;
        private Renderer _beard;
        private Renderer _eyebrows;

        [SetUp]
        public void SetUp()
        {
            _head = new GameObject("head");
            _hair = Styled("HairSocket", "Mohawk");
            _beard = Styled("FacialHairSocket", "FullBeard");
            _eyebrows = Styled("EyebrowSocket", "Brows");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_head);

        private Renderer Styled(string socket, string style)
        {
            var parent = new GameObject(socket);
            parent.transform.SetParent(_head.transform, false);
            var mesh = new GameObject(style, typeof(MeshFilter), typeof(MeshRenderer));
            mesh.transform.SetParent(parent.transform, false);
            return mesh.GetComponent<Renderer>();
        }

        private static Cloth Load(string item)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Clothing + item + ".prefab");
            Assert.That(prefab, Is.Not.Null, item);
            return prefab.GetComponent<Cloth>();
        }

        [Test]
        public void HardHat_HidesHeadHairOnly()
        {
            Cloth hat = Load("HardHatYellow");

            Assert.That(hat.HidesHair, Is.True);
            Assert.That(hat.Occludes(_hair), Is.True);
            Assert.That(hat.Occludes(_beard), Is.False);
            Assert.That(hat.Occludes(_eyebrows), Is.False);
        }

        [Test]
        public void Glasses_LeaveHairVisible()
        {
            Cloth glasses = Load("GlassesSecurityHud");

            Assert.That(glasses.HidesHair, Is.False);
            Assert.That(glasses.Occludes(_hair), Is.False);
        }
    }
}
