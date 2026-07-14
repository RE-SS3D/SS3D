using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// Personal digital assistant with an internal ID card slot.
    /// </summary>
    public class PDA : Item, IIdentification
    {
        private AttachedContainer attachedContainer;

        [HideInInspector] public Item StartingIDCard;

        public override void OnStartServer()
        {
            base.OnStartServer();
            EnsureStartingIdCardInserted();
        }

        [Server]
        public void EnsureStartingIdCardInserted()
        {
            if (attachedContainer == null)
            {
                attachedContainer = GetComponent<AttachedContainer>();
            }

            if (StartingIDCard == null || attachedContainer == null)
            {
                return;
            }

            if (GetInsertedIdCard() != null)
            {
                return;
            }

            attachedContainer.AddItem(StartingIDCard);
        }

        public IDCard GetInsertedIdCard()
        {
            if (attachedContainer == null)
            {
                return null;
            }

            return attachedContainer.Items.FirstOrDefault() as IDCard;
        }

        public bool HasPermission(IDPermission permission)
        {
            IDCard idCard = GetInsertedIdCard();
            return idCard != null && idCard.HasPermission(permission);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();
            return interactions.ToArray();
        }
    }
}
