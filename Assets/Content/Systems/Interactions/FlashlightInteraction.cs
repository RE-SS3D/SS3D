using SS3D.Content.Items.Functional.Tools;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class FlashlightInteraction : IInteraction
    {
        public Sprite icon;
        private class ClientFlashlightInteraction : IClientInteraction
        {
            public bool ClientStart(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is Flashlight flashlight)
                {
                    flashlight.light.enabled = !flashlight.light.enabled;
                }

                return false;
            }

            public bool ClientUpdate(InteractionEvent interactionEvent)
            {
                throw new System.NotImplementedException();
            }

            public void ClientCancel(InteractionEvent interactionEvent)
            {
                throw new System.NotImplementedException();
            }
        }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return new ClientFlashlightInteraction();
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Flashlight flashlight)
            {
                if (flashlight.light.enabled)
                {
                    return "Turn Off";
                }
            }

            return "Turn On";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
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