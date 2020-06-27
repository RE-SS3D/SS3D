using System;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Substances;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class DispenseSubstanceInteraction : IInteraction
    {
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

            var provider = interactionEvent.Source as IGameObjectProvider;
            if (provider == null)
            {
                return false;
            }
            if (provider.GameObject.GetComponent<SubstanceContainer>() == null)
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
                    container.AddSubstance(Substance.Substance, Substance.Moles);
                    container.MarkDirty();
                }
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}