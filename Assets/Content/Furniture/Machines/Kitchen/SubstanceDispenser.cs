using System;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Substances;
using UnityEngine.Assertions;

namespace SS3D.Content.Furniture.Generic
{
    public class SubstanceDispenser : InteractionTargetBehaviour
    {
        /// <summary>
        /// The name of the interaction
        /// </summary>
        public string InteractionName;
        /// <summary>
        /// What should be dispensed
        /// </summary>
        public string Substance;
        /// <summary>
        /// How much should be dispensed
        /// </summary>
        public float Moles;
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            var registry = FindObjectOfType<SubstanceRegistry>();
            Substance substance = registry.FromId(Substance);
            Assert.IsNotNull(substance);
            return new IInteraction[]
            {
                new DispenseSubstanceInteraction
                {
                    RangeCheck = true, Substance = new SubstanceEntry(substance, Moles), Name = String.IsNullOrWhiteSpace(InteractionName) ? "Fill" : InteractionName
                } 
            };
        }
    }
}