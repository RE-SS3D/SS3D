using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Roles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Identification
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public sealed class IDCard : Item, IIdentification
    {
        /// <summary>
        /// Name of the owner of this ID card.
        /// </summary>
        [SerializeField]
        private string _ownerName;

        /// <summary>
        /// Name of the role of this ID card.
        /// </summary>
        [SerializeField]
        private string _roleName;

        [SyncObject]
        private readonly SyncList<IDPermission> _permissions = new();

        public string OwnerName
        {
            get => _ownerName;
            set => _ownerName = value;
        }

        public string RoleName
        {
            get => _roleName;
            set => _roleName = value;
        }

        public bool HasPermission(IDPermission permission) => permission == null || _permissions.Contains(permission);

        public void AddPermission(IDPermission permission)
        {
            _permissions.Add(permission);
            _permissions.Dirty(permission);
        }

        public void RemovePermission(IDPermission permission)
        {
            _permissions.Remove(permission);
            _permissions.Dirty(permission);
        }

        [NotNull]
        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}