using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    /// <summary>
    /// Tier 3 combine pour: held source container onto another container (same grammar as disposal drop-in).
    /// </summary>
    public sealed class PourSubstanceInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction
    {
        public float TransferVolumeMl { get; set; } = SubstanceConstants.DefaultTransferVolumeMl;

        public string GetName(InteractionEvent interactionEvent) => "Pour";

        public string GetGenericName() => "PourSubstance";

        public int Priority => 35;

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Combine;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Transfer;

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = targetEvent.WithSource(originEvent.Source);
            return CanInteract(combined);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (interactionEvent.Source is not IGameObjectProvider provider)
            {
                return false;
            }

            SubstanceContainer source = provider.GameObject.GetComponent<SubstanceContainer>();
            if (source == null || source.Locked || source.IsEmpty)
            {
                return false;
            }

            SubstanceContainer target = interactionEvent.Target.GetComponent<SubstanceContainer>();
            if (target == null || target == source || target.Locked || target.RemainingVolumeMl <= SubstanceConstants.VolumeEpsilonMl)
            {
                return false;
            }

            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not IGameObjectProvider provider)
            {
                return false;
            }

            SubstanceContainer source = provider.GameObject.GetComponent<SubstanceContainer>();
            SubstanceContainer target = interactionEvent.Target.GetComponent<SubstanceContainer>();
            if (source == null || target == null)
            {
                return false;
            }

            source.TransferVolume(target, TransferVolumeMl);
            return false;
        }
    }
}
