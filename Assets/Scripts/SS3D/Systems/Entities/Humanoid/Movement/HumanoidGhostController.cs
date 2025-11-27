using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for ghost biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    public class HumanoidGhostController : NetworkActor
    {
        private float _moveSpeed = 5f; // Movement speed

        private float _rotationSpeed = 10f; // Rotation speed for smooth turning

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
            }
        }

        protected void Update()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;

            transform.position += movement * (_moveSpeed * Time.deltaTime);

            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }
    }
}
