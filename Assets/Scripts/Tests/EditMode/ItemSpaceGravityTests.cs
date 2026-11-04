using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Items;

namespace EditorTests
{
    /// <summary>
    /// Loose items follow the ragdoll zero-g rule (#123): gravity over floor support, none in
    /// confirmed open space, and unchanged while support is unknown (no map / AOI lag).
    /// </summary>
    public class ItemSpaceGravityTests
    {
        [Test]
        public void SupportedFloorKeepsGravity()
        {
            Assert.AreEqual(true, Item.UseGravityForSupport(HumanoidSupportState.Supported));
        }

        [Test]
        public void ConfirmedOpenSpaceDisablesGravity()
        {
            Assert.AreEqual(false, Item.UseGravityForSupport(HumanoidSupportState.Unsupported));
        }

        [Test]
        public void UnknownSupportLeavesGravityAlone()
        {
            Assert.IsNull(Item.UseGravityForSupport(HumanoidSupportState.Unknown));
        }
    }
}
