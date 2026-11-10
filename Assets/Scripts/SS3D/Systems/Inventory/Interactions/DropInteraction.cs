using SS3D.Data;
using System;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class DropInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;
        /// <summary>
        /// The maximum angle of surface the item will allow being dropped on
        /// </summary>
        private float _maxSurfaceAngle = 10;

        /// <summary>
        /// Only raycast the default layer for seeing if we are vision blocked. Resolved on first use, because
        /// <see cref="LayerMask.GetMask"/> can't run in a static field initializer.
        /// </summary>
        private static int _defaultMask = -1;

        private static int DefaultMask => _defaultMask >= 0 ? _defaultMask : _defaultMask = LayerMask.GetMask("Default");

        /// <summary>How far the eye's LOS hit may land from the clicked point. Exact today; #192 / plan Q5 may add slack.</summary>
        public const float LineOfSightTolerance = 0.1f;

        /// <summary>
        /// Distance from <paramref name="point"/> to where a ray from <paramref name="viewOrigin"/> toward it first hits
        /// the Default layer; infinity when it hits nothing. Also used by the server's rejection diagnostics (P1.9).
        /// </summary>
        public static float LineOfSightMiss(Vector3 viewOrigin, Vector3 point)
        {
            Vector3 direction = (point - viewOrigin).normalized;
            if (!LineOfSight.TryGetFirstHit(viewOrigin, direction, Mathf.Infinity, DefaultMask, out RaycastHit hit))
            {
                return float.PositiveInfinity;
            }

            return Vector3.Distance(point, hit.point);
        }

        public int Priority => 5;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        public string GetGenericName() => "Drop";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Discard);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
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

            if (!interactionEvent.HasPoint)
            {
                return false;
            }

            // Confirm the entity's ViewPoint can see the drop point: the LOS hit must land near the clicked point,
            // because interaction rays are cast from the camera, not from the view point.
            if (LineOfSightMiss(entity.ViewPoint.transform.position, interactionEvent.Point) > LineOfSightTolerance)
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

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // Face entity yaw while keeping the prefab's authored rest pitch/roll (side-lying guns/tools).
            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            Quaternion rotation = hand.ItemInHand.GetWorldFacing(entity.transform.eulerAngles.y);
            hand.PlaceHeldItemOutOfHand(interactionEvent.Point, rotation);

            return false;
        }
    }
}