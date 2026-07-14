using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Help-intent chest-zone defibrillation. Restarts a stopped heart before brain death; mis-shock burns a beating heart.
    /// </summary>
    public class DefibrillatorInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public Sprite Icon;

        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Defibrillate";

        public string GetGenericName() => "Defibrillate";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = new(originEvent.Source, targetEvent.Target, targetEvent.Point, targetEvent.Normal);
            if (!CanInteract(combined))
            {
                return false;
            }

            HumanHealthController health = ResolveHealth(combined);
            if (health == null)
            {
                return false;
            }

            return ZoneTargetResolver.TryResolveZone(combined.Point, health, out BodyZone zone)
                && zone == BodyZone.Chest;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!TryGetDefibrillatorItem(interactionEvent.Source, out _))
            {
                return false;
            }

            if (ResolveHealth(interactionEvent) == null)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HumanHealthController health = ResolveHealth(interactionEvent);
            if (health == null)
            {
                return false;
            }

            if (!ZoneTargetResolver.TryResolveZone(interactionEvent.Point, health, out BodyZone zone))
            {
                return false;
            }

            health.TryDefibrillate(zone);
            return false;
        }

        private static bool TryGetDefibrillatorItem(IInteractionSource source, out Item item)
        {
            item = null;
            if (source is not IGameObjectProvider provider)
            {
                return false;
            }

            item = provider.GameObject.GetComponent<Item>();
            return item != null && item.GetComponent<DefibrillatorItemExtension>() != null;
        }

        private static HumanHealthController ResolveHealth(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not IGameObjectProvider targetBehaviour)
            {
                return null;
            }

            Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
            return entity != null ? entity.GetComponentInChildren<HumanHealthController>() : null;
        }
    }

    /// <summary>
    /// Adds defibrillator interactions when this item is used on viable targets.
    /// </summary>
    public class DefibrillatorItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly DefibrillatorInteraction Interaction = new();

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            Item item = GetComponent<Item>();

            foreach (IInteractionTarget target in targets)
            {
                if (Interaction.CanInteract(new InteractionEvent(item, target)))
                {
                    interactions.Add(new InteractionEntry(target, Interaction));
                }
            }
        }
    }
}
