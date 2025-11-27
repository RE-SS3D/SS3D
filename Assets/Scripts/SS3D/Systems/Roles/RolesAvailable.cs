using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// List of all roles available in the gamemode
    /// </summary>
    [CreateAssetMenu(fileName = "Role Data", menuName = "Roles/Roles")]
    public class RolesAvailable : ScriptableObject
    {
        [field: SerializeField]
        public List<RolesData> Roles { get; private set; }
    }
}
