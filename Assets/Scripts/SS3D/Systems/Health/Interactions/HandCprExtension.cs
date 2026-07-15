using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Adds empty-hand CPR from hand sources when the patient needs oxy assistance.
    /// </summary>
    public sealed class HandCprExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private CprInteraction _interaction;

        private void Awake()
        {
            _interaction = new CprInteraction();
        }

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            if (_interaction == null)
            {
                return;
            }

            Hand hand = GetComponent<Hand>();
            if (!hand.IsEmpty())
            {
                return;
            }

            foreach (IInteractionTarget target in targets)
            {
                if (_interaction.CanInteract(new InteractionEvent(hand, target)))
                {
                    interactions.Add(new InteractionEntry(target, _interaction));
                }
            }
        }
    }
}
