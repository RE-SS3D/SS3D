using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Help-intent, zone-targeted interaction that stops bleeding on one body zone.
    /// </summary>
    public class BandageInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public Sprite Icon;

        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Bandage";

        public string GetGenericName() => "Bandage";

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

            if (!ZoneTargetResolver.TryResolveZone(combined.Point, health, out BodyZone zone))
            {
                return false;
            }

            return health.Snapshot.IsZoneBleeding(zone);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!TryGetBandageItem(interactionEvent.Source, out _))
            {
                return false;
            }

            HumanHealthController health = ResolveHealth(interactionEvent);
            if (health == null || !health.Snapshot.IsBleeding)
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

            health.ApplyTreatment(zone, stopBleeding: true);

            if (TryGetBandageItem(interactionEvent.Source, out Item item))
            {
                item.Delete();
            }

            return false;
        }

        private static bool TryGetBandageItem(IInteractionSource source, out Item item)
        {
            item = null;
            if (source is not IGameObjectProvider provider)
            {
                return false;
            }

            item = provider.GameObject.GetComponent<Item>();
            return item != null && item.GetComponent<BandageItemExtension>() != null;
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
}
