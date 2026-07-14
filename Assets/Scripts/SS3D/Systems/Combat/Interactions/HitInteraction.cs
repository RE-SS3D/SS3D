using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Data.Generated;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Interaction to hit another player. Full zone targeting ships in Phase 4.
    /// </summary>
    public class HitInteraction : IInteraction, IClientInteractionSource, IIntentRestrictedInteraction
    {
        public string Name;
        public Sprite Icon;

        public IntentType AllowedIntent => IntentType.Harm;

        public int Priority => 100;

        public string GetName(InteractionEvent interactionEvent) => "Hit";

        public string GetGenericName() => "Hit";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Nuke);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && source is Hand)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
                if (entity == null)
                {
                    return false;
                }

                HumanHealthController health = entity.GetComponentInChildren<HumanHealthController>();
                if (health == null)
                {
                    return false;
                }

                return InteractionExtensions.RangeCheck(interactionEvent);
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && source is Hand)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
                HumanHealthController health = entity.GetComponentInChildren<HumanHealthController>();
                health?.ApplyDamage(BodyZone.Chest, 50f, 0f);
            }

            return false;
        }
    }
}
