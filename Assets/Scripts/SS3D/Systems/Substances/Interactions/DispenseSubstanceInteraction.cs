using System;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Substances
{
    public class DispenseSubstanceInteraction : IInteraction
    {
        public event Action OnInteractionInvalid;

        public string Name { get; set; } = "Dispense";
        /// <summary>
        /// The substance to dispense
        /// </summary>
        public SubstanceEntry Substance { get; set; }
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;
        /// <summary>
        /// If a range check should be automatically performed
        /// </summary>
        public bool RangeCheck { get; set; }

        public virtual string GetGenericName()
        {
            return Name;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return Name;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (RangeCheck && !InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IGameObjectProvider provider = interactionEvent.Source as IGameObjectProvider;
            if (provider == null)
            {
                return false;
            }

            SubstanceContainer container = provider.GameObject.GetComponent<SubstanceContainer>();
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
                SubstanceContainer container = provider.GameObject.GetComponent<SubstanceContainer>();
                if (container != null)
                {
                    container.AddSubstance(Substance.Substance, Substance.MilliMoles);
                    container.SetDirty();
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