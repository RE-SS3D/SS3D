using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    public class StructuralAudioTests
    {
        private const string SoundsPath = "Assets/Content/Data/Databases/Sounds.asset";
        private const string WallsFolder = "Assets/Content/Data/TileMap/Resources/Structures/Walls/";

        private static readonly Vector3 Listener = Vector3.zero;

        [TestCase(TileObjectGenericType.Wall, TileObjectSpecificType.Steel, StructuralMaterial.Metal)]
        [TestCase(TileObjectGenericType.Table, TileObjectSpecificType.Steel, StructuralMaterial.Metal)]
        [TestCase(TileObjectGenericType.Table, TileObjectSpecificType.Wood, StructuralMaterial.Wood)]
        [TestCase(TileObjectGenericType.Table, TileObjectSpecificType.Poker, StructuralMaterial.Wood)]
        [TestCase(TileObjectGenericType.Table, TileObjectSpecificType.Glass, StructuralMaterial.Glass)]
        [TestCase(TileObjectGenericType.Door, TileObjectSpecificType.None, StructuralMaterial.Metal)]
        [TestCase(TileObjectGenericType.None, TileObjectSpecificType.None, StructuralMaterial.Metal)]
        public void Material_DerivesFromTypes(TileObjectGenericType generic, TileObjectSpecificType specific,
            StructuralMaterial expected)
        {
            Assert.AreEqual(expected, StructuralMaterials.Resolve(generic, specific, StructuralMaterialOverride.Derived));
        }

        [TestCase(StructuralMaterialOverride.Metal, StructuralMaterial.Metal)]
        [TestCase(StructuralMaterialOverride.Wood, StructuralMaterial.Wood)]
        [TestCase(StructuralMaterialOverride.Glass, StructuralMaterial.Glass)]
        public void Material_OverrideWinsOverTypes(StructuralMaterialOverride materialOverride, StructuralMaterial expected)
        {
            Assert.AreEqual(expected,
                StructuralMaterials.Resolve(TileObjectGenericType.Table, TileObjectSpecificType.Glass, materialOverride));
        }

        [Test]
        public void Material_NullObjectIsMetal()
        {
            Assert.AreEqual(StructuralMaterial.Metal, StructuralMaterials.Resolve((TileObjectSo)null));
        }

        [TestCase("SteelWindow")]
        [TestCase("SteelWindowReinforced")]
        public void Material_WindowsAreGlassDespiteTheirSteelWallType(string asset)
        {
            TileObjectSo window = AssetDatabase.LoadAssetAtPath<TileObjectSo>(WallsFolder + asset + ".asset");

            Assert.IsNotNull(window);
            Assert.AreEqual(StructuralMaterial.Glass, StructuralMaterials.Resolve(window));
        }

        [TestCase(StructuralMaterial.Metal, StructuralDamageSource.Ranged, StructuralImpactCue.Ricochet)]
        [TestCase(StructuralMaterial.Wood, StructuralDamageSource.Ranged, StructuralImpactCue.Hit)]
        [TestCase(StructuralMaterial.Glass, StructuralDamageSource.Ranged, StructuralImpactCue.Hit)]
        [TestCase(StructuralMaterial.Metal, StructuralDamageSource.Melee, StructuralImpactCue.Hit)]
        public void Cue_OnlyBulletsOffMetalRicochet(StructuralMaterial material, StructuralDamageSource source,
            StructuralImpactCue expected)
        {
            Assert.AreEqual(expected, StructuralImpactCues.Resolve(StructuralIntegrityStage.Intact,
                StructuralIntegrityStage.Damaged, material, source));
        }

        [Test]
        public void Cue_StrainPlaysOnceOnReachingCracked()
        {
            Assert.AreEqual(StructuralImpactCue.Strain, StructuralImpactCues.Resolve(StructuralIntegrityStage.Damaged,
                StructuralIntegrityStage.Cracked, StructuralMaterial.Metal, StructuralDamageSource.Melee));
            Assert.AreEqual(StructuralImpactCue.Hit, StructuralImpactCues.Resolve(StructuralIntegrityStage.Cracked,
                StructuralIntegrityStage.Cracked, StructuralMaterial.Metal, StructuralDamageSource.Melee));
        }

        [Test]
        public void Cue_KillingBlowLeavesTheSoundToTheBreak()
        {
            Assert.AreEqual(StructuralImpactCue.None, StructuralImpactCues.Resolve(StructuralIntegrityStage.Cracked,
                StructuralIntegrityStage.Destroyed, StructuralMaterial.Glass, StructuralDamageSource.Melee));
        }

        [Test]
        public void Clips_BulletOnWoodDoesNotRicochet()
        {
            string[] clips = StructuralAudioPresenter.ClipsFor(StructuralImpactCue.Hit, StructuralMaterial.Wood);

            Assert.IsEmpty(clips.Intersect(CombatAudioTrackIds.SurfaceHit));
        }

        [Test]
        public void Clips_EachMaterialHasItsOwnBreak()
        {
            string[] metal = StructuralAudioPresenter.ClipsFor(StructuralImpactCue.Break, StructuralMaterial.Metal);
            string[] wood = StructuralAudioPresenter.ClipsFor(StructuralImpactCue.Break, StructuralMaterial.Wood);
            string[] glass = StructuralAudioPresenter.ClipsFor(StructuralImpactCue.Break, StructuralMaterial.Glass);

            Assert.IsEmpty(metal.Intersect(wood));
            Assert.IsEmpty(metal.Intersect(glass));
            Assert.IsEmpty(wood.Intersect(glass));
        }

        [Test]
        public void Clips_EveryIdIsRegisteredInTheSoundDatabase()
        {
            string database = System.IO.File.ReadAllText(SoundsPath);
            var cues = new[]
            {
                StructuralImpactCue.Hit, StructuralImpactCue.Ricochet, StructuralImpactCue.Strain, StructuralImpactCue.Break,
            };
            var materials = new[] { StructuralMaterial.Metal, StructuralMaterial.Wood, StructuralMaterial.Glass };

            IEnumerable<string> ids = cues
                .SelectMany(cue => materials.SelectMany(material => StructuralAudioPresenter.ClipsFor(cue, material)))
                .Concat(StructuralAudioTrackIds.BlastNear)
                .Concat(StructuralAudioTrackIds.WreckageThud)
                .Append(StructuralAudioTrackIds.BlastFar);

            foreach (string id in ids.Distinct())
                StringAssert.Contains("_key: " + id, database);
        }

        [Test]
        public void BlastRange_SwitchesToTheDistantRumble()
        {
            Assert.IsFalse(StructuralAudioPresenter.IsDistantBlast(StructuralAudioPresenter.BlastFarDistance));
            Assert.IsTrue(StructuralAudioPresenter.IsDistantBlast(StructuralAudioPresenter.BlastFarDistance + 0.1f));
        }

        [Test]
        public void Scheduler_LoneBreakPlaysInFullAfterTheHold()
        {
            var scheduler = new StructuralSoundScheduler();
            scheduler.OnBreak(Vector3.one, StructuralMaterial.Glass, false, 0f);

            Assert.IsEmpty(Tick(scheduler, 0.01f));

            StructuralSound sound = Tick(scheduler, StructuralSoundScheduler.BreakHoldSeconds).Single();
            Assert.AreEqual(StructuralSoundKind.Break, sound.Kind);
            Assert.AreEqual(StructuralMaterial.Glass, sound.Material);
            Assert.AreEqual(1f, sound.Volume);
        }

        [Test]
        public void Scheduler_WreckageThudsWhenItLands()
        {
            var scheduler = new StructuralSoundScheduler();
            scheduler.OnBreak(Vector3.one, StructuralMaterial.Metal, true, 0f);

            Assert.AreEqual(StructuralSoundKind.Break, Tick(scheduler, 0.05f).Single().Kind);
            Assert.IsEmpty(Tick(scheduler, 0.05f + StructuralSoundScheduler.WreckageLandSeconds - 0.01f));
            Assert.AreEqual(StructuralSoundKind.Thud,
                Tick(scheduler, 0.05f + StructuralSoundScheduler.WreckageLandSeconds).Single().Kind);
        }

        [Test]
        public void Scheduler_BlastCapsDebrisToTheVoicesNearestTheListener()
        {
            var scheduler = new StructuralSoundScheduler();
            for (int i = 1; i <= 8; i++)
                scheduler.OnBreak(new Vector3(i * 2f, 0f, 0f), StructuralMaterial.Metal, true, 0f);

            scheduler.OnBlast(0.001f);
            List<StructuralSound> heard = TickUntil(scheduler, 2f);

            Assert.AreEqual(StructuralSoundScheduler.MaxDebrisVoices, heard.Count);
            CollectionAssert.AreEqual(new[] { 2f, 4f, 6f }, heard.Select(sound => sound.Position.x));
        }

        [Test]
        public void Scheduler_DebrisIsQuieterThanALoneBreakAndHasNoThud()
        {
            var scheduler = new StructuralSoundScheduler();
            scheduler.OnBreak(Vector3.one, StructuralMaterial.Metal, true, 0f);
            scheduler.OnBlast(0.001f);

            StructuralSound sound = TickUntil(scheduler, 2f).Single();

            Assert.AreEqual(StructuralSoundKind.Break, sound.Kind);
            Assert.Less(sound.Volume, 1f);
        }

        [Test]
        public void Scheduler_BreakJustAfterTheBlastStillCountsAsDebris()
        {
            var scheduler = new StructuralSoundScheduler();
            scheduler.OnBlast(0f);
            scheduler.OnBreak(Vector3.one, StructuralMaterial.Metal, false, 0.02f);

            Assert.Less(TickUntil(scheduler, 2f).Single().Volume, 1f);
        }

        [Test]
        public void Scheduler_FrameHitchDoesNotTurnDebrisIntoFullBreaks()
        {
            var scheduler = new StructuralSoundScheduler();
            for (int i = 1; i <= 8; i++)
                scheduler.OnBreak(new Vector3(i, 0f, 0f), StructuralMaterial.Metal, true, 0f);
            scheduler.OnBlast(0.001f);

            // The first tick after the blast lands well past its window.
            List<StructuralSound> heard = Tick(scheduler, 0.5f);
            heard.AddRange(TickUntil(scheduler, 3f, 0.6f));

            Assert.AreEqual(StructuralSoundScheduler.MaxDebrisVoices, heard.Count);
        }

        [Test]
        public void Scheduler_BreakLongAfterABlastPlaysInFull()
        {
            var scheduler = new StructuralSoundScheduler();
            scheduler.OnBlast(0f);
            TickUntil(scheduler, 1f);
            scheduler.OnBreak(Vector3.one, StructuralMaterial.Metal, false, 1f);

            Assert.AreEqual(1f, TickUntil(scheduler, 2f, 1f).Single().Volume);
        }

        private static List<StructuralSound> Tick(StructuralSoundScheduler scheduler, float now)
        {
            var due = new List<StructuralSound>();
            scheduler.Tick(now, Listener, due);
            return due;
        }

        private static List<StructuralSound> TickUntil(StructuralSoundScheduler scheduler, float end, float start = 0f)
        {
            var due = new List<StructuralSound>();
            for (float now = start; now <= end; now += 0.016f)
                scheduler.Tick(now, Listener, due);
            return due;
        }
    }
}
