using JetBrains.Annotations;
using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : NetworkActor, IInteractionTarget
    {
        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        [NotNull]
        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction()
                {
                    Name = "Detonate", Interact = Detonate, CanInteractCallback = CanDetonate, RangeCheck = true,
                },
            };
        }

        private bool CanDetonate(InteractionEvent interactionEvent)
        {
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            return source is NukeCard _ && inRange;
        }

        private void Detonate(InteractionEvent interactionEvent, InteractionReference interactionReference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (source is not NukeCard _ || target is not Nuke nuke)
            {
                return;
            }

            PlayerSubSystem playerSubSystem = Subsystems.Get<PlayerSubSystem>();
            new NukeDetonateEvent(nuke, playerSubSystem.GetCkey(source.GetComponentInParent<Entity>().Owner)).Invoke(this);
        }
    }
}
