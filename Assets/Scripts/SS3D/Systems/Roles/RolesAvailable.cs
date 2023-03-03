using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    [CreateAssetMenu(fileName = "Role Data", menuName = "Roles/Roles")]
    public class RolesAvailable : ScriptableObject
    {
        public List<RolesData> roles;
    }
}
