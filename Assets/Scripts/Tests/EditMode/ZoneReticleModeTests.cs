using NUnit.Framework;
using SS3D.Interactions;
using SS3D.UI.MainHud.Components;

namespace EditorTests
{
    public class ZoneReticleModeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void HarmShowsReticleWithoutLabel(bool holdsZoneTreatment)
        {
            ZoneReticleMode.Resolve(IntentType.Harm, holdsZoneTreatment, out bool visible, out bool showLabel);
            Assert.IsTrue(visible);
            Assert.IsFalse(showLabel);
        }

        [Test]
        public void HelpWithTreatmentShowsReticleAndLabel()
        {
            ZoneReticleMode.Resolve(IntentType.Help, true, out bool visible, out bool showLabel);
            Assert.IsTrue(visible);
            Assert.IsTrue(showLabel);
        }

        [Test]
        public void HelpWithoutTreatmentHidesReticle()
        {
            ZoneReticleMode.Resolve(IntentType.Help, false, out bool visible, out bool showLabel);
            Assert.IsFalse(visible);
            Assert.IsFalse(showLabel);
        }
    }
}
