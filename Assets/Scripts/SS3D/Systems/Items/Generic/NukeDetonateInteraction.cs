using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Items.Generic
{
    /// <summary>
    /// Boom.
    /// </summary>
    public class NukeDetonateInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Detonate Nuke";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Nuke);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            return source is NukeCard && inRange;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (target == null)
            {
                return false;
            }

            if (source is NukeCard _ && target is Nuke nuke)
            {
                nuke.Detonate();
             
                // TODO: Improve this
                PlayerSystem playerSystem = SystemLocator.Get<PlayerSystem>();
                new NukeDetonateEvent(nuke, playerSystem.GetCkey(source.GetComponentInTree<Entity>().Owner)).Invoke(this);
            }

            return false;
        }
    }
}