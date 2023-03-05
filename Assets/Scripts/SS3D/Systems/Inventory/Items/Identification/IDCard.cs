using System.Collections.Generic;
using System.Linq;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Roles;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Coimbra;
using SS3D.Logging;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class IDCard : Item, IIdentification
    {
        private string ownerName;
        private string roleName;

        public string OwnerName
        {
            get => ownerName;
            set => ownerName = value;
        }

        public string RoleName
        {
            get => roleName;
            set => roleName = value;
        }

        [SyncObject]
        private readonly SyncList<IDPermission> permissions = new SyncList<IDPermission>();
        
        public bool HasPermission(IDPermission permission)
        {
            if (permission == null)
            {
                return true;
            }

            return permissions.Contains(permission);
        }

        public void AddPermission(IDPermission permission)
        {
            permissions.Add(permission);
            permissions.Dirty(permission);
        }

        public void RemovePermission(IDPermission permission)
        {
            permissions.Remove(permission);
            permissions.Dirty(permission);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}