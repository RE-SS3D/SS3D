using JetBrains.Annotations;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Items.Identification
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    [RequireComponent(typeof(AttachedContainer))]
    public sealed class Pda : Item, IIdentification
    {
        /// <summary>
        /// The permission related to this PDA.
        /// </summary>
        public IDPermission Permission;

        [SerializeField]
        private AttachedContainer _attachedContainer;

        [FormerlySerializedAs("StartingIDCard")]
        [HideInInspector] 
        public Item IDCard;

        protected override void OnStart()
        {
            base.OnStart();

            _attachedContainer = GetComponent<AttachedContainer>();

            if (IDCard != null && _attachedContainer != null)
            {
                _attachedContainer.Container.AddItem(IDCard);
            }
        }

        public bool HasPermission(IDPermission permission)
        {
            if (_attachedContainer == null)
            {
                return false;
            }

            IDCard idCard = _attachedContainer.Container.Items.FirstOrDefault() as IDCard;

            return idCard != null && idCard.HasPermission(permission);
        }

        [NotNull]
        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}