using System;

namespace SS3D.Data.Persistence
{
    [Serializable]
    public sealed class SavedPermissionsPayload
    {
        public SavedPermissionRecord[] records = Array.Empty<SavedPermissionRecord>();
    }
}
