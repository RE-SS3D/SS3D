using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Interaction to change music on Jukeboxes and boomboxes.
    /// </summary>
    public class ChangeMusicInteraction : IInteraction
    {
        public string Name;
        public Sprite Icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Change Music";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
                return boom.InteractionIcon;

            return null;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }

                return boom.AudioOn;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                boom.ChangeCurrentMusic();
            }

            return false;
        }
    }
}