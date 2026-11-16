using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    /// <summary>Tier 2 armed pour from source container into a targeted container.</summary>
    public class TransferSubstanceInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction
    {
        public float TransferVolumeMl { get; set; } = SubstanceConstants.DefaultTransferVolumeMl;

        public string GetName(InteractionEvent interactionEvent) => "Transfer";

        public string GetGenericName() => "TransferSubstance";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = targetEvent.WithSource(originEvent.Source);
            return CanInteract(combined);
        }

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Transfer;

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
