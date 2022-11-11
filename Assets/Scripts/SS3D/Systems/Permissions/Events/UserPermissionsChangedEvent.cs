using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Permissions.Events
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