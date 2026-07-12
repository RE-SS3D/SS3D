using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// Physical ID token bound to a server-side crew record. Carries no independent access state.
    /// </summary>
    public class IDCard : Item, IIdentification
    {
        [SyncVar]
        private uint _boundRecordId;

        [SyncVar]
        private string _ownerName;

        [SyncVar]
        private string _roleName;

        [SyncVar]
        private Department _department;

        public CrewRecordId BoundRecordId => new(_boundRecordId);

        public string OwnerName => _ownerName;

        public string RoleName => _roleName;

        public Department Department => _department;

        public bool HasPermission(IDPermission permission)
        {
            if (permission == null)
            {
                return true;
            }

            AccessMask required = IdAccessPermissionMapper.FromLegacyPermission(permission);
            if (required.IsNone)
            {
                return true;
            }

            if (!SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                return false;
            }

            HumanInventory inventory = GetComponentInParent<HumanInventory>();
            if (inventory == null)
            {
                return false;
            }

            AccessMask credential = idAccess.ResolveCredentialMask(inventory);
            return credential.HasAll(required);
        }

        [Server]
        public void ServerBind(CrewRecord record)
        {
            _boundRecordId = record.Id.Value;
            ServerRefreshView(record);
        }

        [Server]
        public void ServerRefreshView(CrewRecord record)
        {
            _ownerName = record.Name;
            _roleName = record.JobName;
            _department = record.Department;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();
            return interactions.ToArray();
        }
    }
}
