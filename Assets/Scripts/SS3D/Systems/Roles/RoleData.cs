using SS3D.Data.AssetDatabases;
using System;
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
        [SerializeField]
        private string _roleName;
        [SerializeField]
        private ObjectAssetReference _pdaAsset;
        [SerializeField]
        private ObjectAssetReference _idCardAsset;
        [SerializeField]
        private List<IDPermission> _permissions = new();
        [SerializeField]
        private RoleLoadout _loadout;

        public string Name => _roleName;

        public ObjectAssetReference IDCardAsset => _idCardAsset;

        public ObjectAssetReference PDAAsset => _pdaAsset;

        public List<IDPermission> Permissions => _permissions;

        public RoleLoadout Loadout => _loadout;
    }
}