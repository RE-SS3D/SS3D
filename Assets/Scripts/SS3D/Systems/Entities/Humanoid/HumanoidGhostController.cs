using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for ghost biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    public class HumanoidGhostController : HumanoidController
    {
        public override void OnStartClient()
        {
            base.OnStartClient();
            // Base OnAwake/Setup already resolves HumanoidBodyStateMachine — do not redeclare
            // _bodyStateMachine here (Unity serializes the duplicate name and warns on rebuild).
            BodyStateMachine?.SetFloating(true);
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected override void ProcessCharacterMovement()
        {
            ProcessPlayerInput();

            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                RotatePlayerToMovement();
                MovePlayer();
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero);
            }
        }

        /// <summary>
        /// Server: moves the ghost to <paramref name="position"/>. Ghost movement is driven by the
        /// owning client, so the owner is told to move too; a server-only write would be overwritten
        /// by the owner's next position update.
        /// </summary>
        [Server]
        public void ServerTeleport(Vector3 position)
        {
            transform.position = position;
            if (Owner != null && Owner.IsValid)
            {
                TargetTeleport(Owner, position);
            }
        }

        [TargetRpc]
        private void TargetTeleport(NetworkConnection conn, Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            transform.position += TargetMovement * ((_movementSpeed) * Time.deltaTime);
        }
    }
}
