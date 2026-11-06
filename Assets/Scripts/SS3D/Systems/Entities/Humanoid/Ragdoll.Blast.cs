using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// A blast is one velocity change, not a wind: the atmos field would overwrite it within a tick
    /// and caps it at <see cref="SS3D.Systems.Atmospherics.AtmosWind.MaxSpeed"/>. It shares wind's
    /// routing because it has the same problem: only the physics authority can move a body.
    /// </summary>
    public partial class Ragdoll
    {
        /// <summary>The top of combat.md §5's knockdown range.</summary>
        private const float BlastKnockdownSeconds = 3f;

        /// <summary>How quickly a shove on an upright body dies away, per second.</summary>
        private const float ShoveDecayRate = 5f;

        /// <summary>Upward speed per unit of planar speed: a body sliding on its limbs stops within a metre.</summary>
        private const float LaunchLift = 0.25f;

        private Vector3 _blastShove;

        /// <param name="velocity">Planar velocity, from <see cref="SS3D.Systems.StructuralDamage.BlastPush"/>.</param>
        /// <param name="knockdown">Whether this blast floors the body; anything else only shoves an upright one.</param>
        [Server]
        public void ServerApplyBlast(Vector3 velocity, bool knockdown)
        {
            if (knockdown)
            {
                ServerBlastKnockdown();
            }
            // Boots hold against moving air, but a blast that floors the wearer ends that (armor.md §3).
            else if (TryGetComponent(out EvaGearController gear) && gear.ServerAnchored())
            {
                return;
            }

            if (_deathRagdoll || !Owner.IsValid)
            {
                FeelBlast(velocity);
            }
            else
            {
                RpcBlast(Owner, velocity, _presentation);
            }
        }

        private void ServerBlastKnockdown()
        {
            if (!IsKnockedDown)
            {
                // Same recovery rule as wind: a body thrown over a hole does not stand up on nothing.
                _windKnockdown = true;
                ServerKnockdown(BlastKnockdownSeconds);
            }
            // Do not convert an existing health-owned timeless collapse to a timed knockdown.
            else if (_isKnockdownTimed)
            {
                _knockdownTimer = Mathf.Max(_knockdownTimer, BlastKnockdownSeconds);
            }
        }

        [TargetRpc]
        private void RpcBlast(NetworkConnection connection, Vector3 velocity, BodyPresentationState presentation)
        {
            if (this == null) return;
            // The SyncVar may land after this RPC, and force on still-kinematic bones is lost.
            if (presentation != BodyPresentationState.Locomotion) ApplyPresentation(presentation, false);
            FeelBlast(velocity);
        }

        private void FeelBlast(Vector3 velocity)
        {
            // Human.prefab ships this component disabled until something needs its updates.
            enabled = true;
            if (_currentState == RagdollState.Ragdoll)
                LaunchBones(velocity);
            else
                _blastShove = velocity;
        }

        private void LaunchBones(Vector3 velocity)
        {
            if (!IsRagdollPhysicsAuthority()) return;
            // Bones in space have no gravity, so any lift there would carry them off the station's plane for good.
            if (HasFloorSupport()) velocity.y = velocity.magnitude * LaunchLift;

            foreach (Transform part in _ragdollParts)
            {
                Rigidbody body = part.GetComponent<Rigidbody>();
                if (!body.isKinematic) body.AddForce(velocity, ForceMode.VelocityChange);
            }
        }

        private void TickBlastShove(float seconds)
        {
            if (_blastShove == Vector3.zero) return;
            if (_currentState != RagdollState.Walking || !IsRagdollPhysicsAuthority() || _characterController == null)
            {
                _blastShove = Vector3.zero;
                return;
            }

            MoveUpright(_blastShove * seconds);
            _blastShove *= Mathf.Exp(-ShoveDecayRate * seconds);
            if (_blastShove.sqrMagnitude < 0.01f) _blastShove = Vector3.zero;
        }
    }
}
