using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Can be used to wrap an interaction, adding additional consequences
    /// </summary>
    public abstract class Requirement : Interaction
    {
        public IInteraction Interaction { get; set; }

        protected Requirement(IInteraction interaction)
        {
            Interaction = interaction;
        }

        /// <summary>
        /// Checks if the requirement is satisfied
        /// </summary>
        public abstract bool SatisfiesRequirement(InteractionEvent interactionEvent);

        /// <summary>
        /// Applies the requirement once it completes
        /// </summary>
        /// <param name="interactionEvent"></param>
        protected virtual void ApplyRequirement(InteractionEvent interactionEvent) { }

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return Interaction.CreateClient(interactionEvent);
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return Interaction.GetName(interactionEvent);
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Interaction.GetIcon(interactionEvent);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            return SatisfiesRequirement(interactionEvent) && Interaction.CanInteract(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction.Start(interactionEvent, reference)) return true;

            ApplyRequirement(interactionEvent);
            return false;
        }

        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction.Update(interactionEvent, reference)) return true;

            ApplyRequirement(interactionEvent);
            return false;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Interaction.Cancel(interactionEvent, reference);
        }
    }
}