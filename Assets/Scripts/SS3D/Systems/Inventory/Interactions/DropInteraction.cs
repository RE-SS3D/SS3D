using System;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class DropInteraction : Interaction
    {
        /// <summary>
        /// The maximum angle of surface the item will allow being dropped on
        /// </summary>
        private float _maxSurfaceAngle = 10;

        /// <summary>
        /// Only raycast the default layer for seeing if we are vision blocked
        /// </summary>
        private LayerMask _defaultMask = LayerMask.GetMask("Default");

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIcons.Discard;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            if (!entity)
            {
                return false;
            }

            // Confirm the entities ViewPoint can see the drop point
            Vector3 direction = (interactionEvent.Point - entity.ViewPoint.transform.position).normalized;
            bool raycast = Physics.Raycast(entity.ViewPoint.transform.position, direction, out RaycastHit hit, 
                Mathf.Infinity, _defaultMask);
            if (!raycast)
            {
                return false;
            }

            // Confirm raycasted hit point is near the interaction point.
            // This is necessary because interaction rays are casted from the camera, not from view point
            if (Vector3.Distance(interactionEvent.Point, hit.point) > 0.1)
            {
                return false;
            }
            
            // Consider if the surface is facing up
            float angle = Vector3.Angle(interactionEvent.Normal, Vector3.up);
            if (angle > _maxSurfaceAngle)
            {
                return false;
            }
            
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // rotate the item based on the facing direction of the entity
            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            Quaternion rotation = Quaternion.Euler(0, entity.transform.eulerAngles.y, 0);
            
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            hand.PlaceHeldItemOutOfHand(interactionEvent.Point, rotation);
            
            return false;
        }
    }
}