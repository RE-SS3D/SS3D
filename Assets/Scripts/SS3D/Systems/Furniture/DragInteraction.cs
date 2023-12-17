using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Furniture
{
    public class DragInteraction : Interaction
    {
        /// <summary>
        /// If the interaction should be range limited
        /// </summary>
        public bool RangeCheck { get; set; } = true;


        public override string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target.GetGameObject().TryGetComponent<Draggable>(out var draggable))
            {
                return draggable.Dragged ? "Drop" : "Drag";
            }

            return null;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return InteractionIcons.Discard;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (RangeCheck && !InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return true;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (!interactionEvent.Target.GetGameObject().TryGetComponent<Draggable>(out var draggable))
            {
                return false;
            }

            draggable.SetDrag(!draggable.Dragged, interactionEvent.Source.GameObject.transform);

            return false;
        }
    }
}
