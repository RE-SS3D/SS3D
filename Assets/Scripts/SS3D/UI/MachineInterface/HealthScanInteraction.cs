using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Help-intent "Scan vitals" from a held health scanner onto any character with a health
    /// controller, the patient's own body included. Resolves instantly (virology.md §5).
    /// </summary>
    public sealed class HealthScanInteraction : IInteraction, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Scan vitals";

        public string GetGenericName() => "Scan vitals";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Examine;

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            return CanInteract(targetEvent.WithSource(originEvent.Source));
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (ResolveScanner(interactionEvent.Source) == null || ResolvePatient(interactionEvent) == null)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HealthScannerInterfaceController scanner = ResolveScanner(interactionEvent.Source);
            HumanHealthController patient = ResolvePatient(interactionEvent);
            if (scanner != null && patient != null)
            {
                scanner.ServerScan(interactionEvent, patient);
            }

            return false;
        }

        private static HealthScannerInterfaceController ResolveScanner(IInteractionSource source)
        {
            return source is IGameObjectProvider provider && provider.GameObject != null
                ? provider.GameObject.GetComponent<HealthScannerInterfaceController>()
                : null;
        }

        private static HumanHealthController ResolvePatient(InteractionEvent interactionEvent)
        {
            if (interactionEvent?.Target is not IGameObjectProvider target || target.GameObject == null)
            {
                return null;
            }

            Entity entity = target.GameObject.GetComponentInParent<Entity>();
            return entity != null ? entity.GetComponentInChildren<HumanHealthController>() : null;
        }
    }
}
