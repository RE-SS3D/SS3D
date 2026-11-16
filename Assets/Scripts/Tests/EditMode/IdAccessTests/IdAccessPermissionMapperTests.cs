using NUnit.Framework;
using SS3D.Systems.IdAccess;

namespace EditorTests
{
    public class IdAccessPermissionMapperTests
    {
        [Test]
        public void FromLegacyPermissionName_MapsGeneralToStandardCrew()
        {
            var permission = UnityEngine.ScriptableObject.CreateInstance<SS3D.Systems.IDPermission>();
            permission.Name = "General";

            AccessMask mask = IdAccessPermissionMapper.FromLegacyPermission(permission);

            Assert.IsTrue(mask.HasAll(AccessPresets.StandardCrew));
        }

        [Test]
        public void FromLegacyPermissionName_MapsSecurityToSecurityOfficerPreset()
        {
            var permission = UnityEngine.ScriptableObject.CreateInstance<SS3D.Systems.IDPermission>();
            permission.Name = "Security";

            AccessMask mask = IdAccessPermissionMapper.FromLegacyPermission(permission);

            Assert.IsTrue(mask.HasAll(AccessMask.FromLevels(AccessLevel.Security, AccessLevel.Armory)));
        }
    }
}
