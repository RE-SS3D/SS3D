using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    internal static class MedicalInteractionUtility
    {
        public static HumanHealthController ResolveHealth(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not IGameObjectProvider targetBehaviour)
            {
                return null;
            }

            Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
            return entity != null ? entity.GetComponentInChildren<HumanHealthController>() : null;
        }

        public static bool TryGetTreatmentItem<TExtension>(IInteractionSource source, out Item item) where TExtension : Component
        {
            item = null;
            if (source is not IGameObjectProvider provider)
            {
                return false;
            }

            item = provider.GameObject.GetComponent<Item>();
            return item != null && item.GetComponent<TExtension>() != null;
        }

        public static bool TryResolveZone(InteractionEvent interactionEvent, HumanHealthController health, out BodyZone zone)
        {
            return ZoneTargetResolver.TryResolveZone(interactionEvent.Point, health, out zone);
        }

        public static bool TryResolveChestZone(InteractionEvent interactionEvent, HumanHealthController health, out BodyZone zone)
        {
            if (!TryResolveZone(interactionEvent, health, out zone))
            {
                return false;
            }

            return zone == BodyZone.Chest;
        }

        public static bool TryResolveHeadZone(InteractionEvent interactionEvent, HumanHealthController health, out BodyZone zone)
        {
            if (!TryResolveZone(interactionEvent, health, out zone))
            {
                return false;
            }

            return zone == BodyZone.Head;
        }

        public static void ConsumeTreatmentItem(Item item)
        {
            item?.Delete();
        }
    }
}
