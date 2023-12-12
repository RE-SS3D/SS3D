using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Interaction to change music on Jukeboxes and boomboxes.
    /// </summary>
    public class ChangeMusicInteraction : Interaction
    {
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Change Music";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
                return boom.interactionIcon;
            return null;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }
                return boom.radioOn;
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                boom.ChangeCurrentMusic();
            }
            return false;
        }

        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}
