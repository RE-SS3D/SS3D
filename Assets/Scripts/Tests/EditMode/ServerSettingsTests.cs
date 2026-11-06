using NUnit.Framework;
using SS3D.Systems.Rounds;

namespace EditorTests
{
    public class ServerSettingsTests
    {
        [TestCase(0, 90, false)]
        [TestCase(257, 90, false)]
        [TestCase(32, 0, false)]
        [TestCase(32, 1441, false)]
        [TestCase(1, 1, true)]
        [TestCase(256, 1440, true)]
        public void ValidatesServerResourceLimits(int cap, int minutes, bool expected)
        {
            ServerSettings settings = ServerSettings.Default;
            settings.PlayerCap = cap;
            settings.RoundLengthMinutes = minutes;
            Assert.AreEqual(expected, settings.IsValid);
        }

        [Test]
        public void ClassicMiniStationIsTheDefaultMap()
        {
            Assert.AreEqual("classic-ministation",
                RoundSubSystem.DefaultStation(new[] { "AtmosTest", "ministation", "classic-ministation" }));
        }

        [Test]
        public void DefaultMapFallsBackToTheFirstSavedMap()
        {
            Assert.AreEqual("AtmosTest", RoundSubSystem.DefaultStation(new[] { "AtmosTest", "ministation" }));
            Assert.AreEqual("", RoundSubSystem.DefaultStation(System.Array.Empty<string>()));
        }
    }
}
