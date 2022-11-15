using SS3D.Systems.Furniture;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Systems.GameModes.Events;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class NukeDetonateInteraction : IInteraction
    {
        public Sprite Icon;
    
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientDelayedInteraction();
        }
    
        public string GetName(InteractionEvent interactionEvent)
        {
            return "Detonate Nuke";
        }
    
        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon;
        }                                               
    
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (source is not NukeCard _)
            {
                return false;
            }

            if (!inRange)
            {
                return false;
            }

            return true;
        }
    
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (source is NukeCard _ && target is Nuke nuke)
            {
                nuke.Detonate();
                new NukeDetonateEvent(nuke).Invoke(this);
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