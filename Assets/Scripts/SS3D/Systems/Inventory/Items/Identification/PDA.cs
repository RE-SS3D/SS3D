using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Identification
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class Pda : Item, IIdentification
    {
        public IDPermission TestPermission;

        [HideInInspector]
        public Item StartingIDCard;

        private AttachedContainer _attachedContainer;

        public bool HasPermission(IDPermission permission)
        {
            if (_attachedContainer == null)
            {
                return false;
            }

            IDCard idCard = (IDCard)_attachedContainer.Items.FirstOrDefault();
            if (idCard == null)
            {
                return false;
            }

            return idCard.HasPermission(permission);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }

        protected override void OnStart()
        {
            base.OnStart();

            _attachedContainer = GetComponent<AttachedContainer>();
            if (StartingIDCard)
            {
                _attachedContainer.AddItem(StartingIDCard);
            }
        }
    }
}