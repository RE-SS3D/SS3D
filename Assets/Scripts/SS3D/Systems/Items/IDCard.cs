using SS3D.Data.Enums;
using SS3D.Data;
using SS3D.Systems.Storage.Items;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Roles;
using SS3D.Systems.Traits.TraitCategories;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// What makes you a true Nanotrasen employee
    /// </summary>
    public sealed class IDCard : Item, IIdentification
    {
        [Header("ID Card")] 
        [SyncObject] 
        public readonly SyncList<AccessPermission> AccessPermissions = new();
        
        [SerializeField] 
        [SyncVar]
        private AccessPermissionIDs _accessPermissionType;

        [field: SyncVar] public string IdName { get; set; }

        // TODO: Change this to Jobs when we have it.
        public AccessPermissionIDs AccessPermissionType => _accessPermissionType;

        public override void OnStartServer()
        {
            base.OnStartServer();

            SetPermissionsTo(new List<AccessPermission>
            {
                AssetData.Get(_accessPermissionType) as AccessPermission
            });
        }

        private void SetPermissionsTo(List<AccessPermission> permissions)
        {
            AccessPermissions.Clear();

            foreach (AccessPermission idPermission in permissions)
            {
                AccessPermissions.Add(idPermission);
            }
        }

        public bool HasPermission(AccessPermission accessPermission)
        {
            return accessPermission == null || AccessPermissions.Contains(accessPermission);
        }
    }
}