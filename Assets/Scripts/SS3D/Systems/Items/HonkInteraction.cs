using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class HonkInteraction : IInteraction
    {
        public Sprite Icon;
    
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }
    
        public string GetName(InteractionEvent interactionEvent)
        {
            return "Honk";
        }
    
        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon;
        }                                               
    
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (target is not BikeHorn horn)
            {
                return false;
            }

            if (!inRange)
            {
                return false;
            }

            return !horn.IsHonking();
        }
    
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is BikeHorn horn)
            {
                horn.Honk();
            }
            return false;
        }
    
        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }
    
        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }
    }
}