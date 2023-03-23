using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Substances;
using System;
using UnityEngine;

namespace SS3D.Substances
{
    public class TransferSubstanceInteraction : Interaction
    {
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Transfer";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return null;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var provider = interactionEvent.Source as IGameObjectProvider;
            if (provider == null)
            {
                return false;
            }

            var container = provider.GameObject.GetComponent<SubstanceContainerActor>();
            if (container == null)
            {
                return false;
            }

            if (container.Locked)
            {
                return false;
            }

            if (container.IsEmpty())
            {
                return false;
            }

            return CanInteractCallback.Invoke(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                var container = provider.GameObject.GetComponent<SubstanceContainerActor>();
                if (container != null)
                {
                    var targetContainer = interactionEvent.Target.GetComponent<SubstanceContainerActor>();
                    container.TransferVolume(targetContainer.substanceContainer, 25);
                    container.MarkDirty();
                    targetContainer.MarkDirty();
                }
            }

            return false;
        } 
    }
}