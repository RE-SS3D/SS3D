using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public partial class Ragdoll
    {
        internal static readonly HashSet<Ragdoll> WindBodies = new();
        private bool _windKnockdown;
        private Vector3 _windTarget;
        private Vector3 _windFelt;
        private float _windExpires;

        public override void OnStopNetwork()
        {
            WindBodies.Remove(this);
            base.OnStopNetwork();
        }

        public Vector3 WindSamplePosition => _hips != null && IsKnockedDown ? _hips.position : transform.position;

        [Server]
        public void ServerApplyWind(Vector3 gradient)
        {
            if (gradient.sqrMagnitude < AtmosWind.MinPressure * AtmosWind.MinPressure) return;
            // Do not convert an existing health-owned timeless collapse to a timed knockdown.
            if (!IsKnockedDown && gradient.magnitude >= AtmosWind.KnockdownPressure)
            {
                _windKnockdown = true;
                ServerKnockdown(3f);
            }
            else if (_isKnockdownTimed)
                _knockdownTimer = Mathf.Max(_knockdownTimer, 2f);

            Vector3 wind = AtmosWind.Velocity(gradient);
            if (_deathRagdoll || !Owner.IsValid)
            {
                if (IsKnockedDown) ApplyPresentation(_presentation, false);
                FeelWind(wind);
            }
            else
                RpcWind(Owner, wind, _presentation);
        }

        [TargetRpc]
        private void RpcWind(NetworkConnection connection, Vector3 wind, BodyPresentationState presentation)
        {
            // Round teardown opens the hull, so a reliable gust can land the frame its body despawns.
            if (this == null) return;
            // RPC ordering must not leave the first gust on a still-kinematic body.
            if (presentation != BodyPresentationState.Locomotion) ApplyPresentation(presentation, false);
            FeelWind(wind);
        }

        private void FeelWind(Vector3 wind)
        {
            _windTarget = wind;
            _windExpires = Time.time + AtmosWind.HoldSeconds;
            // Human.prefab ships this component disabled until something needs its updates.
            enabled = true;
        }

        /// <summary>Eases the 5 Hz field and pushes an upright owner a little every frame.</summary>
        private void TickWind(float seconds)
        {
            Vector3 target = Time.time < _windExpires ? _windTarget : Vector3.zero;
            if (target == Vector3.zero && _windFelt == Vector3.zero) return;
            _windFelt = AtmosWind.Follow(_windFelt, target, seconds);
            if (target == Vector3.zero && _windFelt.sqrMagnitude < 0.0001f) _windFelt = Vector3.zero;

            // Living bodies move owner-side; HumanoidPredictedMovement is off on Human.prefab.
            if (_currentState == RagdollState.Walking && IsOwner
                && _characterController != null && _characterController.enabled)
                _characterController.Move(_windFelt * seconds);
        }

        private void FixedUpdate()
        {
            if (_windFelt == Vector3.zero || _currentState != RagdollState.Ragdoll || !IsRagdollPhysicsAuthority())
                return;

            foreach (Transform part in _ragdollParts)
            {
                Rigidbody body = part.GetComponent<Rigidbody>();
                if (!body.isKinematic)
                    body.AddForce(AtmosWind.DragAcceleration(_windFelt, body.linearVelocity), ForceMode.Acceleration);
            }
        }
    }
}
