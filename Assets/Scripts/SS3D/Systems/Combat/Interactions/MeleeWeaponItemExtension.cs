using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Adds melee hit interactions when this item is held and used on viable targets.
    /// </summary>
    public class MeleeWeaponItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        [SerializeField] private MeleeWeaponProfile _profile = MeleeWeaponProfile.Crowbar;

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            Item item = GetComponent<Item>();
            var interaction = new MeleeHitInteraction(_profile);

            foreach (IInteractionTarget target in targets)
            {
                if (interaction.CanInteract(new InteractionEvent(item, target)))
                {
                    interactions.Add(new InteractionEntry(target, interaction));
                }
            }
        }
    }
}
