using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile.MapEditor;
using UnityEditor;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// The chamber intake draws O₂ and N₂, so placed in a room it drains it. It must not pass for a room scrubber.
    /// </summary>
    public class ScrubberIntakeIdentityTests
    {
        private const string ScrubberPath = "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/Scrubber.prefab";
        private const string IntakePath = "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/ScrubberIntake.prefab";

        [Test]
        public void PaletteCallsItAChamberIntake()
        {
            Assert.AreEqual("Chamber Intake", MapEditorCatalog.DisplayLabel(new MapEditorCatalogEntry { AssetName = "ScrubberIntake" }));
        }

        [Test]
        public void IntakeCasingIsTintedApartFromTheScrubber()
        {
            Material scrubberCasing = LoadPrefab(ScrubberPath).GetComponentInChildren<Renderer>().sharedMaterials[0];
            Material intakeCasing = LoadPrefab(IntakePath).GetComponentInChildren<Renderer>().sharedMaterials[0];

            Assert.AreNotEqual(scrubberCasing, intakeCasing);
            Assert.AreNotEqual(scrubberCasing.color, intakeCasing.color);
        }

        [Test]
        public void IntakeHasItsOwnExamineName()
        {
            SimpleExaminable scrubber = LoadPrefab(ScrubberPath).GetComponentInChildren<SimpleExaminable>();
            SimpleExaminable intake = LoadPrefab(IntakePath).GetComponentInChildren<SimpleExaminable>();

            Assert.IsNotNull(intake);
            Assert.AreNotEqual(scrubber.GetData(), intake.GetData());
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            return prefab;
        }
    }
}
