using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Opens the detailed examine view for an examinable target.
    /// </summary>
    public sealed class ExamineInteraction : IInteraction, IClientInteractionSource, IInteractionTierProvider
    {
        public string GetName(InteractionEvent interactionEvent) => "Examine";

        public string GetGenericName() => "Examine";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Instant;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return TryGetExaminable(interactionEvent, out IExaminable examinable) && examinable.GetData() != null;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return false;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            if (TryGetExaminable(interactionEvent, out IExaminable examinable))
            {
                SubSystems.Get<ExamineSubSystem>().ShowDetailedExamine(examinable);
            }

            return null;
        }

        private static bool TryGetExaminable(InteractionEvent interactionEvent, out IExaminable examinable)
        {
            examinable = null;

            if (interactionEvent?.Target == null)
            {
                return false;
            }

            if (interactionEvent.Target is IExaminable directExaminable)
            {
                examinable = directExaminable;
                return true;
            }

            if (interactionEvent.Target is IGameObjectProvider provider)
            {
                examinable = provider.GameObject.GetComponentInParent<IExaminable>();
                return examinable != null;
            }

            return false;
        }
    }
}
