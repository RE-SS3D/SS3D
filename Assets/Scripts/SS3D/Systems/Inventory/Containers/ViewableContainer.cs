using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public class ViewableContainer : NetworkActor, IInteractionTarget
    {
        private AttachedContainer _attachedContainer;

        private IOpenable _openableContainer;

        [NotNull]
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new();

            if (_openableContainer != null && !_openableContainer.IsOpen)
            {
                return interactions.ToArray();
            }

            ViewContainerInteraction viewInteraction = new(_attachedContainer);
            StoreInteraction storeInteraction = new(_attachedContainer);
            TakeFirstInteraction takeFirstInteraction = new(_attachedContainer, 0.2f, 0.2f);
            interactions.Add(viewInteraction);
            interactions.Add(storeInteraction);
            interactions.Add(takeFirstInteraction);
            return interactions.ToArray();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        protected override void OnAwake()
        {
            _attachedContainer = GetComponent<AttachedContainer>();
            _openableContainer = GetComponent<IOpenable>();
        }
    }
}
