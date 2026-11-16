using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using System.Collections.Generic;
using System.Linq;

namespace EditorTests
{
    /// <summary>What the player and onlookers see and hear of shock (health.md §7).</summary>
    public class ShockFeedbackTests
    {
        private static HealthSnapshot WithShock(float shock, HealthState state = HealthState.Healthy, float incoming = 0f)
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.Shock = shock;
            snapshot.IncomingPain = incoming;
            snapshot.State = state;
            return snapshot;
        }

        [Test]
        public void ShockAlertWarnsFromTheWarningLineAndIsCriticalWhenDowned()
        {
            Assert.AreEqual(HealthAlertStackMapper.Severity.None,
                HealthAlertStackMapper.Compute(WithShock(HealthConstants.ShockWarningThreshold - 1f)).Shock);
            Assert.AreEqual(HealthAlertStackMapper.Severity.Warning,
                HealthAlertStackMapper.Compute(WithShock(HealthConstants.ShockWarningThreshold)).Shock);
            Assert.AreEqual(HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(WithShock(HealthConstants.ShockStandThreshold + 1f, HealthState.Downed)).Shock);

            HealthSnapshot critical = WithShock(HealthConstants.ShockCriticalThreshold, HealthState.Critical);
            critical.CriticalFlags = HealthCriticalFlags.HighShock;
            Assert.AreEqual(HealthAlertStackMapper.Severity.Critical, HealthAlertStackMapper.Compute(critical).Shock);
        }

        [Test]
        public void ScreenFeelStepsUpInStages()
        {
            float none = ShockSimulation.FeelIntensity(HealthConstants.ShockWarningThreshold - 1f);
            float light = ShockSimulation.FeelIntensity(HealthConstants.ShockWarningThreshold);
            float nearDown = ShockSimulation.FeelIntensity(HealthConstants.ShockDownedThreshold - 0.1f);
            float down = ShockSimulation.FeelIntensity(HealthConstants.ShockDownedThreshold);
            float top = ShockSimulation.FeelIntensity(HealthConstants.ShockCriticalThreshold * 2f);

            Assert.AreEqual(0f, none);
            Assert.Greater(light, 0f);
            Assert.Greater(nearDown, light);
            Assert.Greater(down, nearDown + 0.05f, "Downed is a step, not a slope");
            Assert.LessOrEqual(top, 1f);

            HealthScreenEffectMapper.Intensities intensities = HealthScreenEffectMapper.Compute(WithShock(HealthConstants.ShockDownedThreshold, HealthState.Downed));
            Assert.AreEqual(down, intensities.Shock, 1e-4f);
        }

        [Test]
        public void PendingPainThatWillDownYouPulsesAndThumps()
        {
            HealthSnapshot goingDown = WithShock(30f, incoming: 35f);
            Assert.IsTrue(ShockSimulation.IsGoingDown(goingDown));
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(goingDown).ShockBuildUp);
            Assert.AreEqual(HealthPersonalAudioMapper.ShockBuildUpHeartbeat, HealthPersonalAudioMapper.ComputeHeartbeatIntensity(goingDown));

            HealthSnapshot survivable = WithShock(30f, incoming: 10f);
            Assert.IsFalse(ShockSimulation.IsGoingDown(survivable));
            Assert.AreEqual(0f, HealthScreenEffectMapper.Compute(survivable).ShockBuildUp);
            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeHeartbeatIntensity(survivable));

            Assert.IsFalse(ShockSimulation.IsGoingDown(WithShock(70f, HealthState.Downed, 10f)), "already down");
        }

        [Test]
        public void ShockMufflesHearingWhileConscious()
        {
            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeMuffle(WithShock(0f)));
            Assert.Greater(HealthPersonalAudioMapper.ComputeMuffle(WithShock(HealthConstants.ShockDownedThreshold, HealthState.Downed)), 0f);
        }

        [Test]
        public void HighShockStumblesAndSlowsTheGait()
        {
            Assert.AreEqual(0f, ShockSimulation.StumbleIntensity(HealthConstants.ShockWarningThreshold - 1f));
            Assert.Greater(ShockSimulation.StumbleIntensity(HealthConstants.ShockWarningThreshold), 0f);

            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            var organs = new List<OrganState> { OrganState.Default(OrganType.Brain), OrganState.Default(OrganType.Heart) };
            HealthSnapshot shaky = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, organs,
                new ShockState { Shock = HealthConstants.ShockWarningThreshold }, HealthState.Healthy);
            Assert.AreEqual(HealthConstants.ShockStumbleSpeedMultiplier, shaky.MovementSpeedMultiplier, 1e-4f);
        }

        [Test]
        public void DownedBodiesGroanAndCriticalOnesGasp()
        {
            Assert.AreEqual(HealthVocalAudio.AmbientVocal.Groan, HealthVocalAudio.AmbientVocalFor(WithShock(65f, HealthState.Downed)));
            Assert.AreEqual(HealthVocalAudio.AmbientVocal.Gasp, HealthVocalAudio.AmbientVocalFor(WithShock(110f, HealthState.Critical)));
            Assert.AreEqual(HealthVocalAudio.AmbientVocal.None, HealthVocalAudio.AmbientVocalFor(WithShock(0f)));

            HealthSnapshot arrest = WithShock(0f, HealthState.CardiacArrest);
            arrest.IsCardiacArrest = true;
            Assert.AreEqual(HealthVocalAudio.AmbientVocal.None, HealthVocalAudio.AmbientVocalFor(arrest), "silence");
        }

        [TestCase(40f, HealthState.Healthy, ExamineHealthKeys.ShockOtherFallback)]
        [TestCase(65f, HealthState.Downed, ExamineHealthKeys.DownedOtherFallback)]
        public void ExamineReadsTheOnlookerLine(float shock, HealthState state, string expected)
        {
            var sections = new List<ExamineSection>();
            CharacterExamineHealthBuilder.AppendSections(WithShock(shock, state), default, false, sections);
            string want = ExamineKeywordFormatter.StripTags(expected);
            Assert.IsTrue(sections.Any(section => ExamineKeywordFormatter.StripTags(section.Text) == want),
                string.Join(" | ", sections.Select(s => s.Text)));
        }

        [Test]
        public void ExamineSaysNothingAboutLowShock()
        {
            var sections = new List<ExamineSection>();
            CharacterExamineHealthBuilder.AppendSections(WithShock(10f), default, false, sections);
            Assert.IsEmpty(sections);
        }
    }
}
