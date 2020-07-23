using System;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Substances;
using UnityEngine;

namespace SS3D.Content.Systems.Substances
{
    public class TransferSubstanceInteraction : IInteraction
    {
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

            var provider = interactionEvent.Source as IGameObjectProvider;
            if (provider == null)
            {
                return false;
            }

            var container = provider.GameObject.GetComponent<SubstanceContainer>();
            if (container == null)
            {
                return false;
            }

            if (container.Locked)
            {
                return false;
            }
            
            if (container.IsEmpty())
            {
                return false;
            }
            
            return CanInteractCallback.Invoke(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                SubstanceContainer container = provider.GameObject.GetComponent<SubstanceContainer>();
                if (container != null)
                {
                    var targetContainer = interactionEvent.Target.GetComponent<SubstanceContainer>();
                    container.TransferVolume(targetContainer, 25);
                    container.MarkDirty();
                    targetContainer.MarkDirty();
                }
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}