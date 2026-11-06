using NUnit.Framework;
using SS3D.UI.Lobby;

namespace SS3D.Tests.EditMode
{
    public sealed class LobbyAdminSettingsPollTests
    {
        [Test]
        public void LoggedInAdminReadsSettings()
        {
            Assert.IsTrue(LobbyUiSubSystem.ShouldReadAdminSettings(canAdmin: true, hasRounds: true, loggedIn: true));
        }

        [Test]
        public void RefusedAdminNameDoesNotReadSettingsDuringDisconnectGrace()
        {
            Assert.IsFalse(LobbyUiSubSystem.ShouldReadAdminSettings(canAdmin: true, hasRounds: true, loggedIn: false));
        }

        [Test]
        public void LoggedInNonAdminDoesNotReadSettings()
        {
            Assert.IsFalse(LobbyUiSubSystem.ShouldReadAdminSettings(canAdmin: false, hasRounds: true, loggedIn: true));
        }
    }
}
