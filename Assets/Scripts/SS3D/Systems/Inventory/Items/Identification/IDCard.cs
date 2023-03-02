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

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class IDCard : Item, IIdentification
    {
        [SerializeField]
        private List<IDPermission> startingPermissions = new List<IDPermission>();

        [SyncObject]
        private readonly SyncList<IDPermission> permissions = new SyncList<IDPermission>();

        protected override void OnStart()
        {
            base.OnStart();

            for (int i = 0; i < startingPermissions.Count; i++)
            {
                permissions.Add(startingPermissions[i]);
                permissions.Dirty(i);
            }
        }

        public bool HasPermission(IDPermission permission)
        {
            return permissions.Contains(permission);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}