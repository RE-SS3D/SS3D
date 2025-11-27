using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Atmospherics
{
    // This handles ragdolling by atmos wind, which I think should be moved elsewhere
    // makes the player ragdoll when there's strong wind
    [RequireComponent(typeof(Ragdoll))]
    public class AtmosRagdoll : NetworkActor
    {
        private readonly float _minVelocity = 10;
        private readonly float _checkInterval = 1f;

        private float _knockdownTime = 3;

        [FormerlySerializedAs("ragdoll")]
        [SerializeField]
        private Ragdoll _ragdoll;

        private float _lastCheck;

        private AtmosEnvironmentSubSystem _atmosSubSystem;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _atmosSubSystem = Subsystems.Get<AtmosEnvironmentSubSystem>();
        }

        protected void Update()
        {
            float time = Time.time;

            // Reduce check interval
            if (_lastCheck + _checkInterval < time)
            {
                _lastCheck = time;

                // Get current tile position
                Vector3 position = transform.position;

                // AtmosObject atmosObject = atmosSystem.GetAtmosTile(position).AtmosObject;
                // ApplyVelocity(atmosObject.Velocity);
            }
        }

        private void ApplyVelocity(Vector2 velocity)
        {
            if (velocity.sqrMagnitude > _minVelocity * _minVelocity)
            {
               // ragdoll.Knockdown(knockdownTime);
            }
        }
    }
}
