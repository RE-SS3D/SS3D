using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;

namespace SS3D.Systems.Health
{

    public class StaminaRequirement : Requirement
    {
        /// <summary>
        /// Indicator of relative amount of stamina required for interaction.
        /// </summary>
        protected Exertion exertion;

        /// <summary>
        /// The stamina controller of the source, if present
        /// </summary>
        private StaminaController _controller;

        private const float STAMINA_COST_PER_EXERTION_STEP = 2.5f;

        public StaminaRequirement(IInteraction interaction, Exertion exertionRequirement) : base(interaction)
        {
            exertion = exertionRequirement;
        }

        /// <summary>
        /// If using this constructor in an inherited class, remember to manually set the exertion variable.
        /// </summary>
        /// <param name="interaction"></param>
        public StaminaRequirement(IInteraction interaction) : base(interaction)
        {
        }

        public override bool SatisfiesRequirement(InteractionEvent interactionEvent)
        {
            // Check to see if this source has a StaminaController component
            IInteractionSource rootSource = InteractionSourceExtension.GetRootSource(interactionEvent.Source);
            _controller = InteractionSourceExtension.GetComponent<StaminaController>(rootSource);

            // Permit this interaction if the source is not subject to stamina, or if it has sufficient to interact.
            return (_controller == null || _controller.CanCommenceInteraction);

        }

        protected override void ApplyRequirement(InteractionEvent interactionEvent)
        {
            if (_controller != null)
            {
                float staminaCost = 0f;
                switch(exertion)
                {
                    case Exertion.None:
                        break;
                    case Exertion.Light:
                        staminaCost = 1 * STAMINA_COST_PER_EXERTION_STEP;
                        break;
                    case Exertion.Moderate:
                        staminaCost = 2 * STAMINA_COST_PER_EXERTION_STEP;
                        break;
                    case Exertion.Heavy:
                        staminaCost = 3 * STAMINA_COST_PER_EXERTION_STEP;
                        break;
                    case Exertion.Extreme:
                        staminaCost = 4 * STAMINA_COST_PER_EXERTION_STEP;
                        break;
                }

                _controller.DepleteStaminaServer(staminaCost);
            }
        }

    }

    public enum Exertion : int
    {
        None = 0,
        Light = 1,
        Moderate = 2,
        Heavy = 3,
        Extreme = 4
    }
}