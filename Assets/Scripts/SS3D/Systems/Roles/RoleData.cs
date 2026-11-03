using SS3D.Data.AssetDatabases;
using SS3D.Systems;
using SS3D.Systems.IdAccess;
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
        private ulong _startingAccessBits;
        [SerializeField]
        private RoleLoadout _loadout;
        [SerializeField, Tooltip("An AntagonistCategories key (\"nukeop\"). Set for antagonist roles, which a " +
             "gamemode assigns and which never appear in the lobby job list. Empty for jobs.")]
        private string _antagonistCategory;

        public string Name => _roleName;

        /// <summary>The <see cref="AntagonistCategories"/> key this role is drawn for; empty for jobs.</summary>
        public string AntagonistCategory => _antagonistCategory ?? string.Empty;

        public bool IsAntagonist => !string.IsNullOrEmpty(_antagonistCategory);

        public ObjectAssetReference IDCardAsset => _idCardAsset;

        public ObjectAssetReference PDAAsset => _pdaAsset;

        public List<IDPermission> Permissions => _permissions;

        public AccessMask StartingAccess
        {
            get
            {
                var mask = new AccessMask(_startingAccessBits);
                return mask.IsNone
                    ? IdAccessPermissionMapper.FromLegacyPermissions(_permissions)
                    : mask;
            }
        }

        public RoleLoadout Loadout => _loadout;
    }
}