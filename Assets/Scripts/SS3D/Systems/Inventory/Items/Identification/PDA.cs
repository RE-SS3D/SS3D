using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Roles;
using SS3D.Traits;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    [RequireComponent(typeof(AttachedContainer))]
    public sealed class PDA : Item, IIdentification
    {
        [SerializeField]
        private IDPermission _testPermission;

        private AttachedContainer _attachedContainer;

        public Item StartingIDCard { get; set; }

        public bool HasPermission(IDPermission permission)
        {
            return _attachedContainer.Items.FirstOrDefault() is IDCard idCard && idCard.HasPermission(permission);
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
