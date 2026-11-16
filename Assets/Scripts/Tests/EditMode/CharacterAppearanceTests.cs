using NUnit.Framework;
using SS3D.Systems.Entities.Character;
using UnityEngine;
using UnityEditor;

namespace EditorTests
{
    public class CharacterAppearanceTests
    {
        [Test]
        public void Default_IsNotSet()
        {
            Assert.That(default(CharacterAppearance).IsSet, Is.False);
        }

        [Test]
        public void Sanitized_IsSetEvenWithTransparentInput()
        {
            CharacterAppearance appearance = new CharacterAppearance { HairColor = new Color32(51, 26, 0, 0) }.Sanitized();

            Assert.That(appearance.IsSet, Is.True);
            Assert.That(appearance.HairColor.a, Is.EqualTo(255));
            Assert.That(appearance.BeardColor.a, Is.EqualTo(255));
        }

        [Test]
        public void PaletteColors_SurviveByteTransport()
        {
            // Regression: FishNet packs Color as hundredths; palette colors must arrive unchanged.
            Color skin = new Color32(140, 87, 60, 255);
            CharacterAppearance appearance = new CharacterAppearance { SkinColor = skin }.Sanitized();
            Assert.That((Color)appearance.SkinColor, Is.EqualTo(skin));
        }

        [Test]
        public void Sanitized_ClampsBodyValuesAndStyles()
        {
            CharacterAppearance appearance = new CharacterAppearance
            {
                Female = 3f,
                Fat = -1f,
                Jaw = float.NaN,
                HairStyle = -4,
                BeardStyle = 2,
            }.Sanitized();

            Assert.That(appearance.Female, Is.EqualTo(1f));
            Assert.That(appearance.Fat, Is.EqualTo(0f));
            Assert.That(appearance.Jaw, Is.EqualTo(0.5f));
            Assert.That(appearance.HairStyle, Is.EqualTo(0));
            Assert.That(appearance.BeardStyle, Is.EqualTo(2));
        }

        [Test]
        public void Sanitized_MakesFaceColorsOpaqueAndRejectsUnknownEyebrowStyles()
        {
            CharacterAppearance appearance = new CharacterAppearance
            {
                SkinColor = new Color32(140, 87, 60, 0),
                EyeColor = new Color32(76, 126, 168, 12),
                EyebrowColor = new Color32(0, 0, 0, 0),
                EyebrowStyle = "not-a-style",
            }.Sanitized();
            Assert.That(appearance.SkinColor, Is.EqualTo(new Color32(140, 87, 60, 255)));
            Assert.That(appearance.EyeColor, Is.EqualTo(new Color32(76, 126, 168, 255)));
            Assert.That(appearance.EyebrowColor.a, Is.EqualTo(255));
            Assert.That(appearance.EyebrowStyle, Is.EqualTo("humaneyebrowsdefault"));
        }

        [TestCase("thin", "humaneyebrowsdainty")]
        [TestCase("thick", "humaneyebrowsbold")]
        [TestCase("none", "none")]
        [TestCase("humaneyebrowsclassic", "humaneyebrowsclassic")]
        public void EyebrowStyles_MigrateExistingPresets(string old, string expected)
        {
            Assert.That(CharacterAppearance.NormalizeEyebrowStyle(old), Is.EqualTo(expected));
        }

        [Test]
        public void FaceTint_IsPerMaterialAndDoesNotChangeOtherCharactersOrSharedAssets()
        {
            const string folder = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/";
            Material skin = AssetDatabase.LoadAssetAtPath<Material>(folder + "HumanSkin.mat");
            Material eyes = AssetDatabase.LoadAssetAtPath<Material>(folder + "HumanEye.mat");
            Color originalSkin = skin.GetColor("_Color");
            Color originalEyes = eyes.GetColor("_BaseColor");
            GameObject first = new("First");
            GameObject second = new("Second");
            try
            {
                MeshRenderer renderer = first.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new Material[] { skin, eyes };
                MeshRenderer other = second.AddComponent<MeshRenderer>();
                other.sharedMaterials = new Material[] { skin, eyes };
                HumanoidStyleApplier.ApplyFace(first, Color.red, Color.blue, null, Color.black);
                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block, 0);
                Assert.That(block.GetColor("_Color"), Is.EqualTo(Color.red));
                renderer.GetPropertyBlock(block, 1);
                Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.blue));
                other.GetPropertyBlock(block, 0);
                Assert.That(block.isEmpty, Is.True);
                Assert.That(skin.GetColor("_Color"), Is.EqualTo(originalSkin));
                Assert.That(eyes.GetColor("_BaseColor"), Is.EqualTo(originalEyes));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [TestCase("  Jane Doe  ", "Jane Doe")]
        [TestCase("<b>Bold</b>", "bBold/b")]
        [TestCase("Line\nBreak", "LineBreak")]
        [TestCase("   ", "")]
        [TestCase(null, "")]
        public void SanitizeName_StripsMarkupControlAndWhitespace(string input, string expected)
        {
            Assert.That(CharacterAppearance.SanitizeName(input), Is.EqualTo(expected));
        }

        [Test]
        public void SanitizeName_TruncatesToMaxLength()
        {
            string name = CharacterAppearance.SanitizeName(new string('a', 100));

            Assert.That(name.Length, Is.EqualTo(CharacterAppearance.MaxNameLength));
        }
    }
}
