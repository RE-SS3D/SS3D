using SS3D.Traits;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// All the relevant data for a role, including it's name, default ID Card and PDA, 
    /// Permissions and Starting Items
    /// </summary>
    [Serializable]     
    [CreateAssetMenu(fileName = "Role Data", menuName = "Roles/RoleData")]
    public class RoleData : ScriptableObject
    {
        [field:SerializeField] 
        public string Name { get; private set; }

        [field:SerializeField] 
        public GameObject PdaPrefab { get; private set; }

        [field:SerializeField] 
        public GameObject IDCardPrefab { get; private set; }

        [field:SerializeField] 
        public List<IDPermission> Permissions { get; private set; }

        [field:SerializeField] 
        public RoleLoadout Loadout { get; private set; }
    }
}
