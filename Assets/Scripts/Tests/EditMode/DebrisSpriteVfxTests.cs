using System.IO;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;

namespace EditorTests
{
    /// <summary>Debris sprite atlas wiring and the burst each material gets.</summary>
    public class DebrisSpriteVfxTests
    {
        [Test]
        public void EveryAtlasSpriteHasASourceFile()
        {
            Assert.AreEqual(DebrisSpriteVfx.Rows, DebrisSpriteVfx.Sprites.Length);
            foreach (string[] row in DebrisSpriteVfx.Sprites)
            {
                Assert.That(row.Length, Is.InRange(1, DebrisSpriteVfx.Columns));
                foreach (string sprite in row)
                    FileAssert.Exists(Path.Combine(DebrisSpriteVfx.SourceFolder, sprite + ".png"));
            }
        }

        [Test]
        public void CatalogPointsAtThePackedAtlas()
        {
            DebrisVfxCatalog catalog = DebrisVfxCatalog.Instance;
            Assert.IsNotNull(catalog, "Run SS3D → Structural Damage → Run Content Prefab Recipes");
            Assert.IsNotNull(catalog.Atlas);
            Assert.AreEqual(DebrisSpriteVfx.Columns * DebrisSpriteVfx.CellSize, catalog.Atlas.width);
            Assert.AreEqual(DebrisSpriteVfx.Rows * DebrisSpriteVfx.CellSize, catalog.Atlas.height);
        }

        [TestCase(StructuralMaterial.Metal, DebrisKind.Metal)]
        [TestCase(StructuralMaterial.Wood, DebrisKind.Wood)]
        [TestCase(StructuralMaterial.Glass, DebrisKind.Glass)]
        public void MaterialPicksItsRow(StructuralMaterial material, DebrisKind expected)
        {
            Assert.AreEqual(expected, DebrisSpriteVfx.ForMaterial(material));
        }

        [Test]
        public void StillAirLeavesDebrisAlone()
        {
            Assert.AreEqual(Vector3.zero, DebrisSpriteVfx.WindDrift(Vector2.zero));
        }

        [Test]
        public void WindCarriesDebrisDownstreamOnTheFloorPlane()
        {
            Vector2 flow = AtmosCellVisualEncoding.EncodeFlow(new Vector3(-3f, 0f, 0f));
            Vector3 drift = DebrisSpriteVfx.WindDrift(flow);
            Assert.Less(drift.x, 0f);
            Assert.AreEqual(0f, drift.y);
            Assert.AreEqual(0f, drift.z, 0.001f);
        }

        [Test]
        public void KnockdownWindIsCappedSoDebrisIsNotFlung()
        {
            Vector3 drift = DebrisSpriteVfx.WindDrift(new Vector2(0f, 1f));
            Assert.AreEqual(DebrisSpriteVfx.MaxCarriedWind * DebrisSpriteVfx.WindCarry, drift.magnitude, 0.001f);
        }

        [Test]
        public void MetalHitsSkipThePanelSprites()
        {
            ParticleSystem burst = DebrisSpriteVfx.Burst(Vector3.zero, Vector3.up, DebrisKind.Metal, 2, 2f, 0.08f,
                firstSprite: DebrisSpriteVfx.MetalHardwareStart);
            if (burst == null)
                Assert.Ignore("No graphics device: debris is client presentation only.");

            try
            {
                float firstFrame = burst.textureSheetAnimation.frameOverTime.constantMin * DebrisSpriteVfx.Columns;
                Assert.AreEqual(DebrisSpriteVfx.MetalHardwareStart, firstFrame, 0.001f);
                StringAssert.StartsWith("ParticleMechanicalJunk",
                    DebrisSpriteVfx.Sprites[(int)DebrisKind.Metal][DebrisSpriteVfx.MetalHardwareStart]);
            }
            finally
            {
                Object.DestroyImmediate(burst.gameObject);
            }
        }

        [TestCase(DebrisKind.Glass)]
        [TestCase(DebrisKind.Electronics)]
        public void BurstDrawsOnlyFromItsKindsSprites(DebrisKind kind)
        {
            ParticleSystem burst = DebrisSpriteVfx.Burst(Vector3.zero, Vector3.up, kind, 6, 2f, 0.1f);
            if (burst == null)
                Assert.Ignore("No graphics device: debris is client presentation only.");

            try
            {
                var sheet = burst.textureSheetAnimation;
                Assert.AreEqual((int)kind, sheet.rowIndex);
                Assert.AreEqual(6, burst.main.maxParticles);
                // Frames are normalised across the row; the top must stay inside the sprites this row holds.
                float lastFrame = sheet.frameOverTime.constantMax * DebrisSpriteVfx.Columns;
                Assert.Less(lastFrame, DebrisSpriteVfx.Sprites[(int)kind].Length);
                Assert.Greater(lastFrame, DebrisSpriteVfx.Sprites[(int)kind].Length - 1);
            }
            finally
            {
                Object.DestroyImmediate(burst.gameObject);
            }
        }
    }
}
