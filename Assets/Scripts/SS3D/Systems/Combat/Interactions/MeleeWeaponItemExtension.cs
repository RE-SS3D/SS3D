using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Dedicated melee profile when this item is held. Prefer this over improvised fallback on <see cref="Item"/>.
    /// </summary>
    public class MeleeWeaponItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        [SerializeField] private MeleeWeaponProfile _profile = MeleeWeaponProfile.Crowbar;

        public MeleeWeaponProfile Profile => _profile;

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            if (!TryGetComponent(out Item item))
            {
                return;
            }

            var interaction = new MeleeHitInteraction(_profile);
            if (!interaction.CanStartSwing(item))
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
