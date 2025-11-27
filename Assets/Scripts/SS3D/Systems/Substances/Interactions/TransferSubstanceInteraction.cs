using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Substances;
using SS3D.Systems.Interactions;
using System;
using UnityEngine;

namespace SS3D.Substances
{
    public class TransferSubstanceInteraction : IInteraction
    {
        public InteractionType InteractionType => InteractionType.None;

        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        public string GetGenericName() => "Transfer";

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Transfer";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IGameObjectProvider provider = interactionEvent.Source;
            if (provider is null || provider.GameObject.TryGetComponent(out SubstanceContainer container))
            {
                return false;
            }

            if (container.Locked)
            {
                return false;
            }

            if (container.IsEmpty)
            {
                return false;
            }

            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not IGameObjectProvider provider || !provider.GameObject.TryGetComponent(out SubstanceContainer container))
            {
                return false;
            }

            SubstanceContainer targetContainer = interactionEvent.Target.GetComponent<SubstanceContainer>();
            container.TransferVolume(targetContainer, 25);
            container.SetDirty();
            targetContainer.SetDirty();

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
