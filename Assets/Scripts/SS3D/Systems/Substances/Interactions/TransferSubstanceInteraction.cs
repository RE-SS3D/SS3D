using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Substances;
using System;
using UnityEngine;

namespace SS3D.Substances
{
    public class TransferSubstanceInteraction : IInteraction
    {
        public string Name;
        public Sprite Icon;
        /// <summary>
        /// Checks if the interaction should be possible
        /// </summary>
        public Predicate<InteractionEvent> CanInteractCallback { get; set; } = _ => true;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Transfer";
        }

        public string GetGenericName() => throw new NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IGameObjectProvider provider = interactionEvent.Source;

            if (provider == null)
            {
                return false;
            }

            SubstanceContainer container = provider.GameObject.GetComponent<SubstanceContainer>();

            if (container == null)
            {
                return false;
            }

            if (container.Locked)
            {
                return false;
            }

            if (container.IsEmpty)
            {
                return false;
            }

            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                var container = provider.GameObject.GetComponent<SubstanceContainer>();

                if (container != null)
                {
                    var targetContainer = interactionEvent.Target.GetComponent<SubstanceContainer>();
                    container.TransferVolume(targetContainer, 25);
                    container.SetDirty();
                    targetContainer.SetDirty();
                }
            }

            return false;
        }
    }
}