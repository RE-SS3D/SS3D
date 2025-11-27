using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using System;
using UnityEngine;

namespace SS3D.Substances
{
    public class DispenseSubstanceInteraction : IInteraction
    {
        public event Action OnInteractionInvalid;

        public string Name { get; set; } = "Dispense";

        public SubstanceEntry Substance { get; set; }

        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        public bool RangeCheck { get; set; }

        public InteractionType InteractionType => InteractionType.None;

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

            IGameObjectProvider provider = interactionEvent.Source;
            if (provider == null || provider.GameObject.TryGetComponent(out SubstanceContainer container))
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
            if (interactionEvent.Source is not IGameObjectProvider provider || !provider.GameObject.TryGetComponent(out SubstanceContainer container))
            {
                return false;
            }

            container.AddSubstance(Substance.Substance, Substance.MilliMoles);
            container.SetDirty();
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => true;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
