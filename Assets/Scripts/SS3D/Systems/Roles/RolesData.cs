using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Represents a role and how many players are allowed at round start
    /// </summary>
    [Serializable]
    public class RolesData
    {
        public RoleData data;
        public int availableRoles;
    }
}
