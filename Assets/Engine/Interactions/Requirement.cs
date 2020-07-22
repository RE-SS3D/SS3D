using UnityEngine;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// Can be used to wrap an interaction, adding additional consequences
    /// </summary>
    public abstract class Requirement : IInteraction
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
        protected virtual void ApplyRequirement(InteractionEvent interactionEvent)
        {
            
        }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return Interaction.CreateClient(interactionEvent);
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return Interaction.GetName(interactionEvent);
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Interaction.GetIcon(interactionEvent);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return SatisfiesRequirement(interactionEvent) && Interaction.CanInteract(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction.Start(interactionEvent, reference)) return true;
            
            ApplyRequirement(interactionEvent);
            return false;

        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Interaction.Update(interactionEvent, reference)) return true;
            
            ApplyRequirement(interactionEvent);
            return false;
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Interaction.Cancel(interactionEvent, reference);
        }
    }
}