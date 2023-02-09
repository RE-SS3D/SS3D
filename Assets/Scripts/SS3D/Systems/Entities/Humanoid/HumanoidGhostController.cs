using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for ghost biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    public class HumanoidGhostController : HumanoidController
    {
        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected override void ProcessCharacterMovement()
        {
            ProcessToggleRun();
            ProcessPlayerInput();

            if (_input.magnitude != 0)
            {
                MoveMovementTarget(_input);
                RotatePlayerToMovement();
                MovePlayer();
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
            }
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            transform.position += _targetMovement * ((_movementSpeed) * Time.deltaTime);
        }
    }

}
