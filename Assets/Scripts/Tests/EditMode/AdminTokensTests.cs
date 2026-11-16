using NUnit.Framework;
using SS3D.Permissions;
using System.Collections.Generic;

namespace SS3D.Tests.EditMode
{
    public sealed class AdminTokensTests
    {
        private static readonly IReadOnlyDictionary<string, string> Tokens =
            AdminTokens.Parse(new[] { "# admins", "", "owner  s3cret", "mod other" });

        [Test]
        public void RegularPlayers_AlwaysAdmitted()
        {
            Assert.IsTrue(AdminTokens.IsAdmitted(ServerRoleTypes.User, "anyone", null, true, Tokens, out _));
            Assert.IsTrue(AdminTokens.IsAdmitted(ServerRoleTypes.None, "owner", null, true, Tokens, out _));
        }

        [Test]
        public void AdminName_NeedsMatchingToken_WhenEnforced()
        {
            Assert.IsTrue(AdminTokens.IsAdmitted(ServerRoleTypes.ServerOwner, "owner", "s3cret", true, Tokens, out _));
            Assert.IsFalse(AdminTokens.IsAdmitted(ServerRoleTypes.ServerOwner, "owner", null, true, Tokens, out _));
            Assert.IsFalse(AdminTokens.IsAdmitted(ServerRoleTypes.ServerOwner, "owner", "s3cre", true, Tokens, out _));
            Assert.IsFalse(AdminTokens.IsAdmitted(ServerRoleTypes.Administrator, "mod", "s3cret", true, Tokens, out _));
        }

        [Test]
        public void AdminName_WithoutTokenLine_IsRejected_WhenEnforced()
        {
            Assert.IsFalse(AdminTokens.IsAdmitted(ServerRoleTypes.ServerOwner, "hostingUser", "x", true, Tokens, out string reason));
            StringAssert.Contains("hostingUser", reason);
        }

        [Test]
        public void NotEnforced_KeepsLanBehaviour()
        {
            Assert.IsTrue(AdminTokens.IsAdmitted(ServerRoleTypes.ServerOwner, "hostingUser", null, false, Tokens, out _));
        }
    }
}
