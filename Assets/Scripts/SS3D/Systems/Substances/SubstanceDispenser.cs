using System;
using SS3D.Interactions;
using SS3D.Substances;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Core;
using SS3D.Systems.Screens;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Content.Furniture.Generic
{
    // This handles dispensing substances into substance containers
    public class SubstanceDispenser : InteractionTargetBehaviour
    {
        /// <summary>
        /// The name of the interaction
        /// </summary>
        public string InteractionName;
        /// <summary>
        /// What should be dispensed
        /// </summary>
        public SubstanceType[] substances;
        /// <summary>
        /// How much should be dispensed
        /// </summary>
        public float amount;

        /// <summary>
        /// Whether the amount is in milliliters, or moles.
        /// </summary>
        public bool useMillilitres = false;

        private SubstancesSubSystem registry;

        protected override void OnStart()
        {
            base.OnStart();
            registry = SubSystems.Get<SubstancesSubSystem>();
            if (registry == null)
            {
                Debug.LogError("SubstanceRegistry not found. Substances will be disabled.");
            }
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (registry == null) return null;

            // Create a separate interaction for each possible substance to dispense
            IInteraction[] interactions = new IInteraction[substances.Length];
            for (int i = 0; i <substances.Length; i++)
            {

                // Retrieve substance from the Registry
                Substance substance = registry.FromType(substances[i]);

                // Ensure the substance was successfully retrieved.
                if (substance == null)
                {
                    // If it isn't, let them know what it is!
                    Debug.LogWarning("No substance in Registry for " + substances[i] + ". Add it.");
                    return null;
                }

                // Determine how many moles to dispense
                float milliMoles;
                if (useMillilitres)
                {
                    milliMoles = amount / substance.MillilitersPerMilliMoles;
                }
                else
                {
                    milliMoles = amount;
                }

                // Add the specific dispence interaction to the list.
                interactions[i] = new DispenseSubstanceInteraction
                {
                    RangeCheck = true,
                    Substance = new SubstanceEntry(substance, milliMoles),
                    Name = String.IsNullOrWhiteSpace(InteractionName) ? "Fill with " + substances[i] : InteractionName + " " + substances[i]
                };

            }

            return interactions;
        }
    }
}