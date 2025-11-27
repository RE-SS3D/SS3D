using System;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Represents a role and how many players are allowed at round start
    /// </summary>
    [Serializable]
    public class RolesData
    {
        [field: SerializeField]
        public RoleData Data { get; private set; }

        [field: SerializeField]
        public int AvailableRoles { get; private set; }
    }
}
