using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class CharacterExamineHealthBuilderTests
    {
        private static HealthDebugDetail HealthyDetail()
        {
            return new HealthDebugDetail
            {
                Brain = OrganState.Default(OrganType.Brain),
                Heart = OrganState.Default(OrganType.Heart),
                LeftLung = OrganState.Default(OrganType.LeftLung),
                RightLung = OrganState.Default(OrganType.RightLung),
                Liver = OrganState.Default(OrganType.Liver),
            };
        }

        [Test]
        public void HealthySnapshotProducesNoSections()
        {
            List<ExamineSection> sections = new();

            CharacterExamineHealthBuilder.AppendSections(
                HealthSnapshot.Default,
                HealthyDetail(),
                includeSelfDetail: true,
                sections);

            Assert.IsEmpty(sections);
        }

        [Test]
        public void OtherUsesSingleConsolidatedAppearanceLine()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsBleeding = true;
            snapshot.BleedingZoneMask = (1 << (int)BodyZone.Chest) | (1 << (int)BodyZone.Head);
            snapshot.SeveredZoneMask = 1 << (int)BodyZone.LeftLeg;

            HealthDebugDetail detail = HealthyDetail();
            detail.LeftArm = new ZoneDamageState
            {
                Severity = WoundSeverity.Wound,
                Brute = 20f,
            };
            detail.Head = new ZoneDamageState
            {
                Severity = WoundSeverity.Wound,
                Brute = 20f,
            };

            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections(snapshot, detail, includeSelfDetail: false, sections);

            Assert.AreEqual(1, sections.Count);
            string line = ExamineKeywordFormatter.StripTags(sections[0].Text);
            Assert.IsTrue(line.StartsWith("He "), line);
            Assert.IsTrue(line.Contains("missing his left leg"), line);
            Assert.IsTrue(line.Contains("hurt"), line);
            Assert.IsTrue(line.Contains("bleeding from the"), line);
            Assert.IsFalse(line.Contains("dizzy"), line);
        }

        [Test]
        public void SelfUsesSameAppearanceShapeWithI()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsBleeding = true;
            snapshot.BleedingZoneMask = (1 << (int)BodyZone.Chest) | (1 << (int)BodyZone.Head);

            HealthDebugDetail detail = HealthyDetail();
            detail.Chest = new ZoneDamageState { Severity = WoundSeverity.Wound };

            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections(snapshot, detail, includeSelfDetail: true, sections);

            Assert.AreEqual(1, sections.Count);
            Assert.AreEqual(
                "I am hurt and bleeding from the head and chest.",
                ExamineKeywordFormatter.StripTags(sections[0].Text));
        }

        [Test]
        public void SelfAddsFeelLinesForInternalState()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.BrainFunctionPercent = 70f;
            snapshot.Pools = new SystemicPools
            {
                BloodVolumeRatio = 0.7f,
                OxyDebt = 0f,
                ToxinConcentration = 0f,
            };

            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections(
                snapshot,
                HealthyDetail(),
                includeSelfDetail: true,
                sections);

            Assert.IsTrue(sections.Any(s => s.Text == "I feel dizzy."));
            Assert.IsTrue(sections.Any(s => s.Text == "I feel weak and lightheaded."));
            Assert.IsFalse(sections.Any(s => s.Text.Contains("Brain")));
            Assert.IsFalse(sections.Any(s => s.Text.StartsWith("He ")));
        }

        [Test]
        public void OtherDoesNotGetFeelLines()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.BrainFunctionPercent = 70f;
            snapshot.Pools = new SystemicPools
            {
                BloodVolumeRatio = 0.7f,
                OxyDebt = 0.5f,
                ToxinConcentration = 0.5f,
            };

            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections(
                snapshot,
                HealthyDetail(),
                includeSelfDetail: false,
                sections);

            Assert.IsEmpty(sections);
        }

        [Test]
        public void DeadUsesIVersusHe()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Dead;
            snapshot.IsConscious = false;

            List<ExamineSection> selfSections = new();
            CharacterExamineHealthBuilder.AppendSections(
                snapshot,
                HealthyDetail(),
                includeSelfDetail: true,
                selfSections);

            List<ExamineSection> otherSections = new();
            CharacterExamineHealthBuilder.AppendSections(
                snapshot,
                HealthyDetail(),
                includeSelfDetail: false,
                otherSections);

            Assert.AreEqual(1, selfSections.Count);
            Assert.AreEqual("I am dead.", ExamineKeywordFormatter.StripTags(selfSections[0].Text));
            Assert.AreEqual(1, otherSections.Count);
            Assert.AreEqual("He is dead.", ExamineKeywordFormatter.StripTags(otherSections[0].Text));
        }

        [Test]
        public void FeelLinesAreCapped()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.BrainFunctionPercent = 20f;
            snapshot.HeartFunctionPercent = 40f;
            snapshot.Pools = new SystemicPools
            {
                BloodVolumeRatio = 0.5f,
                OxyDebt = 0.6f,
                ToxinConcentration = 0.6f,
            };

            HealthDebugDetail detail = HealthyDetail();
            detail.LeftLung = new OrganState { Type = OrganType.LeftLung, FunctionPercent = 40f };
            detail.RightLung = new OrganState { Type = OrganType.RightLung, FunctionPercent = 40f };
            detail.Heart = new OrganState { Type = OrganType.Heart, FunctionPercent = 40f };
            detail.Liver = new OrganState { Type = OrganType.Liver, FunctionPercent = 40f };
            detail.LeftArm = new ZoneDamageState { IsDisabled = true, Severity = WoundSeverity.Disabled };

            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections(snapshot, detail, includeSelfDetail: true, sections);

            // Appearance ("I am hurt.") plus at most MaxFeelLines.
            Assert.LessOrEqual(sections.Count, 1 + CharacterExamineHealthBuilder.MaxFeelLines);
            Assert.IsTrue(sections.Any(s => s.Text.Contains("barely stay conscious") || s.Text.Contains("dizzy")));
        }

        [Test]
        public void NullHealthControllerAppendsNothing()
        {
            List<ExamineSection> sections = new();
            CharacterExamineHealthBuilder.AppendSections((HumanHealthController)null, true, sections);
            Assert.IsEmpty(sections);
        }
    }
}
