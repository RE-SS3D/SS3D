using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;

namespace EditorTests
{
    public class AirLockAreaAccessTests
    {
        [Test]
        public void EditedRoomAccessImmediatelyOverridesDoorSkinAndTracksUndo()
        {
            AreaRecord room = new() { ParentTag = "Command" };
            AccessMask civilian = new(AccessLevel.Civilian);
            Assert.AreEqual(new AccessMask(AccessLevel.Command), AirLockAccessGate.ResolveAreaRequiredAccess(room, civilian));
            room.DefaultRequiredAccess = new AccessMask(AccessLevel.Armory);
            Assert.AreEqual(new AccessMask(AccessLevel.Armory), AirLockAccessGate.ResolveAreaRequiredAccess(room, civilian));
            room.DefaultRequiredAccess = AccessMask.None;
            Assert.AreEqual(new AccessMask(AccessLevel.Command), AirLockAccessGate.ResolveAreaRequiredAccess(room, civilian));
        }

        [Test]
        public void PublicRoomClearsSkinRestrictionButUnassignedDoorKeepsFallback()
        {
            AccessMask security = new(AccessLevel.Security);
            Assert.AreEqual(AccessMask.None, AirLockAccessGate.ResolveAreaRequiredAccess(new AreaRecord(), security));
            Assert.AreEqual(security, AirLockAccessGate.ResolveAreaRequiredAccess(null, security));
        }
    }
}
