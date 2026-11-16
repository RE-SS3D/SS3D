using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Empty-hand fists melee from hand sources.
    /// </summary>
    public class HandMeleeExtension : MonoBehaviour, IInteractionSourceExtension
    {
        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            if (!TryGetComponent(out Hand hand))
            {
                return;
            }

            var interaction = new MeleeHitInteraction(MeleeWeaponProfile.Fists);
            if (!interaction.CanStartSwing(hand))
            {
                return;
            }

            foreach (IInteractionTarget target in targets)
            {
                if (target is not IGameObjectProvider provider)
                {
                    continue;
                }

                Entity entity = provider.GameObject.GetComponentInParent<Entity>();
                if (entity == null || entity.GetComponentInChildren<HumanHealthController>() == null)
                {
                    continue;
                }

                interactions.Add(new InteractionEntry(target, interaction));
            }
        }
    }
}
