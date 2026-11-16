using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public partial class Ragdoll
    {
        /// <summary>Collapsed hips are put this far above the deck so the bones settle onto it.</summary>
        private const float RescuedHipsHeight = 0.3f;

        /// <summary>Every networked body. <see cref="EntitySubSystem"/> keeps them inside the station.</summary>
        internal static IReadOnlyCollection<Ragdoll> All => WindBodies;

        /// <summary>
        /// Server: a body whose hips (collapsed) or root (standing) are above the ceiling or below the deck goes back
        /// onto the nearest floor. Its owner simulates it, so the owner moves it too.
        /// </summary>
        [Server]
        internal void ServerKeepInStation()
        {
            bool collapsed = IsKnockedDown && _hips != null;
            Vector3 position = collapsed ? _hips.position : transform.position;
            if (!StationBoundsRescue.IsOutOfBounds(position)
                || !StationBoundsRescue.TryGetRescuePosition(position, out Vector3 rescue))
            {
                return;
            }

            Vector3 velocity = collapsed && _hips.TryGetComponent(out Rigidbody hips) ? hips.linearVelocity
                : _characterController != null ? _characterController.velocity : Vector3.zero;
            StationBoundsRescue.LogRescue(this, position, velocity, rescue);
            MoveBodyTo(rescue);
            if (Owner.IsValid && !Owner.IsLocalClient)
            {
                TargetMoveBodyTo(Owner, rescue);
            }
        }

        [TargetRpc]
        private void TargetMoveBodyTo(NetworkConnection conn, Vector3 deck) => MoveBodyTo(deck);

        private void MoveBodyTo(Vector3 deck)
        {
            if (IsKnockedDown && _hips != null)
            {
                // The other bones hang off the hips. Write the bodies too: an interpolated body moved only through its
                // Transform can snap back.
                _hips.position = deck + (Vector3.up * RescuedHipsHeight);
                if (_ragdollParts == null)
                {
                    return;
                }

                foreach (Transform part in _ragdollParts)
                {
                    if (!part.TryGetComponent(out Rigidbody body))
                    {
                        continue;
                    }

                    body.position = part.position;
                    if (!body.isKinematic)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                }

                return;
            }

            CharacterController controller = GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            transform.position = deck;
            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }
        }
    }
}
