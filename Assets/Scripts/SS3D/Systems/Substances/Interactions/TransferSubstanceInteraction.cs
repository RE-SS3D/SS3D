using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System;
using UnityEngine;

namespace SS3D.Substances
{
    public class TransferSubstanceInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction
    {
        public string Name;
        public Sprite Icon;
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Transfer";
        }

        public string GetGenericName() => "TransferSubstance";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = new(originEvent.Source, targetEvent.Target, targetEvent.Point, targetEvent.Normal);
            return CanInteract(combined);
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIconLookup.Transfer;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IGameObjectProvider provider = interactionEvent.Source;

            if (provider == null)
            {
                return false;
            }

            SubstanceContainer container = provider.GameObject.GetComponent<SubstanceContainer>();

            if (container == null)
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
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                var container = provider.GameObject.GetComponent<SubstanceContainer>();

                if (container != null)
                {
                    var targetContainer = interactionEvent.Target.GetComponent<SubstanceContainer>();
                    container.TransferVolume(targetContainer, 25);
                    container.SetDirty();
                    targetContainer.SetDirty();
                }
            }

            return false;
        }
    }
}