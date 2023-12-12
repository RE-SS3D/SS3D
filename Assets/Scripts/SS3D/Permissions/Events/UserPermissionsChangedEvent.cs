using Coimbra.Services.Events;
using System.Collections.Generic;

namespace SS3D.Permissions.Events
{
    public partial struct UserPermissionsChangedEvent : IEvent
    {
        public readonly Dictionary<string, ServerRoleTypes> Permissions;

        public UserPermissionsChangedEvent(Dictionary<string, ServerRoleTypes> permissions)
        {
            Permissions = permissions;
        }
    }
}