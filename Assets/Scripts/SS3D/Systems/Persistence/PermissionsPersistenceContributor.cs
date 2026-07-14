using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Permissions;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Persistence
{
    public sealed class PermissionsPersistenceContributor : IPersistenceContributor
    {
        public const string ContributorIdValue = "permissions";

        private readonly Func<PermissionSubSystem> _permissionSubSystemProvider;

        public PermissionsPersistenceContributor(Func<PermissionSubSystem> permissionSubSystemProvider)
        {
            _permissionSubSystemProvider = permissionSubSystemProvider;
        }

        public string ContributorId => ContributorIdValue;

        public PersistenceLayer Layer => PersistenceLayer.ServerMeta;

        public int LoadOrder => 0;

        public object Capture()
        {
            PermissionSubSystem permissionSubSystem = _permissionSubSystemProvider();
            if (permissionSubSystem == null)
            {
                return null;
            }

            return new SavedPermissionsPayload
            {
                records = LegacyPermissionsMigrator.ToRecords(permissionSubSystem.ExportUserPermissions()),
            };
        }

        public void Restore(object data, PersistenceContext context)
        {
            PermissionSubSystem permissionSubSystem = _permissionSubSystemProvider();
            if (permissionSubSystem == null)
            {
                return;
            }

            SavedPermissionsPayload payload = data as SavedPermissionsPayload;
            if (payload?.records is { Length: > 0 })
            {
                permissionSubSystem.ImportUserPermissions(payload.records);
                return;
            }

            if (LegacyPermissionsMigrator.TryLoadFromLegacyTxt(out SavedPermissionsPayload legacyPayload))
            {
                permissionSubSystem.ImportUserPermissions(legacyPayload.records);
            }
        }
    }
}
