using NUnit.Framework;
using SS3D.UI.MainHud.Components;

namespace EditorTests
{
    public class AlertStackAudioMapperTests
    {
        [Test]
        public void NoHazardsProducesNoNewAlert()
        {
            Assert.IsFalse(AlertStackAudioMapper.HasNewAlert(default, default));
        }

        [Test]
        public void HazardAppearingFromNoneIsNewAlert()
        {
            AlertStackState previous = default;
            AlertStackState current = default;
            current.LowOxygen = AlertSeverity.Warning;

            Assert.IsTrue(AlertStackAudioMapper.HasNewAlert(previous, current));
        }

        [Test]
        public void EscalationFromWarningToCriticalIsNotANewAlert()
        {
            AlertStackState previous = default;
            previous.Bleeding = AlertSeverity.Warning;

            AlertStackState current = default;
            current.Bleeding = AlertSeverity.Critical;

            Assert.IsFalse(AlertStackAudioMapper.HasNewAlert(previous, current));
        }

        [Test]
        public void HazardClearingIsNotANewAlert()
        {
            AlertStackState previous = default;
            previous.Dying = AlertSeverity.Critical;

            AlertStackState current = default;

            Assert.IsFalse(AlertStackAudioMapper.HasNewAlert(previous, current));
        }

        [Test]
        public void OneHazardEscalatingWhileAnotherAppearsIsANewAlert()
        {
            AlertStackState previous = default;
            previous.Bleeding = AlertSeverity.Warning;

            AlertStackState current = default;
            current.Bleeding = AlertSeverity.Critical;
            current.CardiacArrest = AlertSeverity.Critical;

            Assert.IsTrue(AlertStackAudioMapper.HasNewAlert(previous, current));
        }

        [Test]
        public void InvincibleBadge_StaysQuiet()
        {
            Assert.IsFalse(AlertStackAudioMapper.HasNewAlert(default, new AlertStackState { Invincible = AlertSeverity.Warning }));
        }
    }
}
