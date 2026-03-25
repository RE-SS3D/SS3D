using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Can be used to wrap an interaction, adding additional consequences
    /// </summary>
    public abstract class Requirement : IInteraction, IClientInteractionSource
    {
        protected Requirement(IInteraction interaction)
        {
            Interaction = interaction;
        }

        protected IInteraction Interaction { get; }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            if (Interaction is IClientInteractionSource clientInteractionSource)
            {
                return clientInteractionSource.CreateClient(interactionEvent);
            }

            return null;
        }

        public string GetName(InteractionEvent interactionEvent) => Interaction.GetName(interactionEvent);

        public abstract string GetGenericName();

        public Sprite GetIcon(InteractionEvent interactionEvent) => Interaction.GetIcon(interactionEvent);

        public bool CanInteract(InteractionEvent interactionEvent) => SatisfiesRequirement(interactionEvent) && Interaction.CanInteract(interactionEvent);

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction.Start(interactionEvent, reference))
            {
                return true;
            }

            ApplyRequirement(interactionEvent);

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction is IDelayedInteraction delayedInteraction && delayedInteraction.Update(interactionEvent, reference))
            {
                return true;
            }

            ApplyRequirement(interactionEvent);

            return false;
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction is IDelayedInteraction delayedInteraction)
            {
                delayedInteraction.Cancel(interactionEvent, reference);
            }
        }

        /// <summary>
        /// Checks if the requirement is satisfied
        /// </summary>
        protected abstract bool SatisfiesRequirement(InteractionEvent interactionEvent);

        /// <summary>
        /// Applies the requirement once it completes
        /// </summary>
        /// <param name="interactionEvent"></param>
        protected virtual void ApplyRequirement(InteractionEvent interactionEvent) { }
    }
}