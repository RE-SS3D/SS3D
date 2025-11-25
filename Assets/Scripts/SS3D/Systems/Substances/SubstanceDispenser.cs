using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Substances;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Content.Furniture.Generic
{
    // This handles dispensing substances into substance containers
    public class SubstanceDispenser : NetworkActor, IInteractionTarget
    {
        /// <summary>
        /// The name of the interaction
        /// </summary>
        [FormerlySerializedAs("InteractionName")]
        [SerializeField]
        private string _interactionName;

        /// <summary>
        /// What should be dispensed
        /// </summary>
        [FormerlySerializedAs("substances")]
        [SerializeField]
        private SubstanceType[] _substances;

        /// <summary>
        /// How much should be dispensed
        /// </summary>
        [FormerlySerializedAs("amount")]
        [SerializeField]
        private float _amount;

        /// <summary>
        /// Whether the amount is in milliliters, or moles.
        /// </summary>
        [FormerlySerializedAs("useMillilitres")]
        [SerializeField]
        private bool _useMillilitres;

        private SubstancesSubSystem _registry;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (_registry == null)
            {
                return null;
            }

            // Create a separate interaction for each possible substance to dispense
            IInteraction[] interactions = new IInteraction[_substances.Length];
            for (int i = 0; i < _substances.Length; i++)
            {
                // Retrieve substance from the Registry
                Substance substance = _registry.FromType(_substances[i]);

                // Ensure the substance was successfully retrieved.
                if (substance == null)
                {
                    // If it isn't, let them know what it is!
                    Debug.LogWarning("No substance in Registry for " + _substances[i] + ". Add it.");
                    return null;
                }

                // Determine how many moles to dispense
                float milliMoles;
                if (_useMillilitres)
                {
                    milliMoles = _amount / substance.MillilitersPerMilliMoles;
                }
                else
                {
                    milliMoles = _amount;
                }

                // Add the specific dispence interaction to the list.
                interactions[i] = new DispenseSubstanceInteraction
                {
                    RangeCheck = true,
                    Substance = new(substance, milliMoles),
                    Name = string.IsNullOrWhiteSpace(_interactionName) ? "Fill with " + _substances[i] : _interactionName + " " + _substances[i],
                };
            }

            return interactions;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _registry = Subsystems.Get<SubstancesSubSystem>();
            if (_registry == null)
            {
                Debug.LogError("SubstanceRegistry not found. Substances will be disabled.");
            }
        }
    }
}
