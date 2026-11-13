using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Empty-hand help from hand sources: CPR when the patient needs oxy assistance, and helping a Downed body up.
    /// </summary>
    public sealed class HandCprExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private CprInteraction _interaction;
        private HelpUpInteraction _helpUp;

        private void Awake()
        {
            _interaction = new CprInteraction();
            _helpUp = new HelpUpInteraction();
        }

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
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
                if (_interaction.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, _interaction));
                }

                if (_helpUp != null && _helpUp.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, _helpUp));
                }
            }
        }
    }
}
