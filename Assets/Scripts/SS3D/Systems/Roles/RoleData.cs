using SS3D.Data.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// All the relevant data for a role, including it's name, default ID Card and PDA, 
    /// Permissions and Starting Items
    /// </summary>
    [Serializable, CreateAssetMenu(fileName = "Role Data", menuName = "Roles/RoleData")]
    public class RoleData : ScriptableObject
    {
        [SerializeField] private string roleName;
        [SerializeField] private ItemId pdaPrefab;
        [SerializeField] private ItemId idCardPrefab;
        [SerializeField] private List<IDPermission> permissions = new List<IDPermission>();
        [SerializeField] private RoleLoadout loadout;

        public string Name { get => roleName; }
        public ItemId IDCardPrefab { get => idCardPrefab; }
        public ItemId PDAPrefab { get => pdaPrefab; }
        public List<IDPermission> Permissions { get => permissions; }
        public RoleLoadout Loadout { get => loadout; }
    }
}
