using NUnit.Framework;
using SS3D.Systems.IdAccess;

namespace EditorTests
{
    public class AreaAccessDefaultsTests
    {
        [Test]
        public void FromParentTag_MapsEngineeringTag()
        {
            AccessMask mask = AreaAccessDefaults.FromParentTag("Engineering");

            Assert.IsTrue(mask.HasAll(AccessMask.FromLevels(AccessLevel.Engineering)));
        }

        [Test]
        public void FromParentTag_ReturnsNoneForUnknownTag()
        {
            Assert.IsTrue(AreaAccessDefaults.FromParentTag("Atmos").IsNone);
        }
    }
}
