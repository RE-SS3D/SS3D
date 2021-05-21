using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Interactions;
using UnityEngine;


namespace SS3D.Content.Systems.Interactions
{
    public class IgniteInteraction : IInteraction
    {
        public Sprite igniteIcon, extinguishIcon;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!(interactionEvent.Target is IIgnitable ignitable))
            {
                return false;
            }

            if (interactionEvent.Source is IIgniter igniter)
            {
                return igniter.CanIgnite && ignitable.CanBeLit;
            }

            return ignitable.Lit;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            IIgnitable ignitable = (IIgnitable)interactionEvent.Target;
            return ignitable.Lit ? extinguishIcon : igniteIcon;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            IIgnitable ignitable = (IIgnitable)interactionEvent.Target;
            return ignitable.Lit ? "Extinguish" : "Ignite";
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (!(interactionEvent.Target is IIgnitable ignitable))
            {
                return false;
            }

            if (ignitable.Lit)
            {
                ignitable.Extinguish();
            }
            else
            {
                ignitable.Ignite();
            }
            return false;
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}
