using JetBrains.Annotations;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Substances;
using System;
using UnityEngine;

namespace SS3D.Systems.Substances.Interactions
{
    public sealed class DispenseSubstanceInteraction : IInteraction
    {
        public string Name { get; set; } = "Dispense";
        /// <summary>
        /// The substance to dispense
        /// </summary>
        public SubstanceEntry Substance { get; set; }
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        private Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;
        /// <summary>
        /// If a range check should be automatically performed
        /// </summary>
        public bool RangeCheck { get; set; }

        public string GetName(InteractionEvent interactionEvent) => Name;

        [CanBeNull]
        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        [CanBeNull]
        public Sprite GetIcon(InteractionEvent interactionEvent) => null;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (RangeCheck && !InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var provider = interactionEvent.Source as IGameObjectProvider;
            if (provider == null)
            {
                return false;
            }

            SubstanceContainer container = provider.ProvidedGameObject.GetComponent<SubstanceContainer>();
            if (container == null)
            {
                return false;
            }

            // You cannot dispense to a container that is already full.
            if (container.RemainingVolume < 0.01f)
            {
                return false;
            }

            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                SubstanceContainer container = provider.ProvidedGameObject.GetComponent<SubstanceContainer>();
                if (container != null)
                {
                    container.AddSubstance(Substance.Substance, Substance.Moles);
                    container.MarkDirty();
                }
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return;
        }
    }
}