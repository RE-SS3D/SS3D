using NUnit.Framework;
using SS3D.Systems.Session;

namespace SS3D.Tests.EditMode
{
    public sealed class ClientSessionAdminSettingsTests
    {
        [Test]
        public void LoggedInAdminReadsSettings()
        {
            Assert.IsTrue(ClientSessionSubSystem.ShouldRequestAdminSettings(isAdmin: true, hasSettingsService: true, loggedIn: true));
        }

        [Test]
        public void RefusedAdminNameDoesNotReadSettingsDuringDisconnectGrace()
        {
            Assert.IsFalse(ClientSessionSubSystem.ShouldRequestAdminSettings(isAdmin: true, hasSettingsService: true, loggedIn: false));
        }

        [Test]
        public void LoggedInNonAdminDoesNotReadSettings()
        {
            Assert.IsFalse(ClientSessionSubSystem.ShouldRequestAdminSettings(isAdmin: false, hasSettingsService: true, loggedIn: true));
        }
    }
}
