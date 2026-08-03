using System;
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Drives the humanoid Animator: movement speed/floating (existing), plus posture, leg limping,
    /// per-arm hold poses, flinch reactions, emotes, and hit/throw one-shots (see issue #1333).
    ///
    /// All parameters are referenced by Animator.StringToHash constant from
    /// <see cref="Animations.Humanoid"/>, so this compiles and runs safely even before the matching
    /// layers/parameters exist on the Animator Controller asset - Unity treats SetX calls on an
    /// unknown parameter hash as a silent no-op (console warning only), not an exception. See
    /// docs/AnimationSystem.md for the full parameter contract and the Unity-side setup required to
    /// actually see any of this play out.
    ///
    /// Most methods here are owner-gated (only the client controlling this entity may drive its own
    /// animator); FishNet's NetworkAnimator (already present on Human/Ghost prefabs) replicates the
    /// resulting parameter changes to observers. The hit trigger is the one exception - it is set
    /// server-side (from HitInteraction, which runs authoritatively on the server) and therefore
    /// needs an explicit ObserversRpc to reach every client, since NetworkAnimator only syncs
    /// owner-driven changes.
    /// </summary>
    public class HumanoidAnimatorController : NetworkActor
    {
        [SerializeField] private HumanoidController _movementController;

        [SerializeField] private Animator _animator;
        [SerializeField] private float _lerpMultiplier;

        [SerializeField] private List<EmoteData> _emotes = new();

        private Posture _currentPosture = Posture.Standing;

        protected override void OnStart()
        {
            base.OnStart();
            SubscribeToEvents();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _movementController.OnSpeedChangeEvent += UpdateMovement;
        }

        private void UnsubscribeFromEvents()
        {
            _movementController.OnSpeedChangeEvent -= UpdateMovement;
        }

        private void UpdateMovement(float speed)
        {
            bool isMoving = speed != 0;
            float currentSpeed = _animator.GetFloat(Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);

            _animator.SetFloat(Animations.Humanoid.MovementSpeed, speed);
        }

        /// <summary>
        /// Sets the current body posture, driving the Sit/Prone animator bools and gating which
        /// emotes are allowed to play. Owner-only.
        /// </summary>
        public void SetPosture(Posture posture)
        {
            if (!IsOwner)
            {
                return;
            }

            _currentPosture = posture;
            _animator.SetBool(Animations.Humanoid.Sit, posture == Posture.Sitting);
            _animator.SetBool(Animations.Humanoid.Prone, posture == Posture.Prone);
        }

        /// <summary>
        /// Sets how much a leg is limping: -1 (left leg hurt) .. 0 (healthy) .. 1 (right leg hurt).
        /// Owner-only. Nothing in the codebase drives this from the Health system yet (today's
        /// FeetController only exposes one combined, non-directional health factor) - it's exposed
        /// here so that plumbing can be added later without changing this contract.
        /// </summary>
        public void SetLegInjury(float value)
        {
            if (!IsOwner)
            {
                return;
            }

            _animator.SetFloat(Animations.Humanoid.LegInjury, Mathf.Clamp(value, -1f, 1f));
        }

        /// <summary>
        /// Sets the hold pose shown on one arm, independently of the other. Owner-only.
        /// </summary>
        public void SetHoldPose(HandSide side, HoldPose pose)
        {
            if (!IsOwner)
            {
                return;
            }

            int parameter = side == HandSide.Left ? Animations.Humanoid.HoldPoseLeft : Animations.Humanoid.HoldPoseRight;
            _animator.SetInteger(parameter, (int)pose);
        }

        /// <summary>
        /// Plays a one-shot additive flinch reaction on the given body region. Owner-only.
        /// </summary>
        public void TriggerFlinch(FlinchRegion region)
        {
            if (!IsOwner)
            {
                return;
            }

            _animator.SetTrigger(GetFlinchParameter(region));
        }

        /// <summary>
        /// Attempts to play the given emote. Returns false without any side effect if the current
        /// posture disallows it (see <see cref="EmoteData.IsAllowedInPosture"/>). Owner-only.
        /// </summary>
        public bool TryPlayEmote(EmoteData emote)
        {
            if (!IsOwner || emote == null || !emote.IsAllowedInPosture(_currentPosture))
            {
                return false;
            }

            _animator.SetInteger(Animations.Humanoid.EmoteIndex, emote.EmoteIndex);
            _animator.SetTrigger(Animations.Humanoid.EmoteTrigger);
            return true;
        }

        /// <summary>
        /// Finds a registered emote by name (case-insensitive), from the list assigned in the
        /// inspector. Used by the "emote" console command to resolve a player-typed name.
        /// </summary>
        public bool TryGetEmote(string emoteName, out EmoteData emote)
        {
            emote = _emotes.Find(e => e != null && string.Equals(e.EmoteName, emoteName, StringComparison.OrdinalIgnoreCase));
            return emote != null;
        }

        /// <summary>
        /// Triggers a one-shot throw animation locally. Owner-only. There is no real throw-item
        /// interaction in the codebase yet (only Hit exists), so this exists purely so the animation
        /// can be exercised/tested via the "throwanim" console command until that gameplay lands.
        /// </summary>
        public void TriggerThrowLocal(HandSide side)
        {
            if (!IsOwner)
            {
                return;
            }

            _animator.SetTrigger(side == HandSide.Left ? Animations.Humanoid.ThrowTriggerLeft : Animations.Humanoid.ThrowTriggerRight);
        }

        /// <summary>
        /// Server-authoritative hit trigger, called from HitInteraction (which runs server-side).
        /// Broadcasts the resulting one-shot swing animation to every observing client.
        /// </summary>
        [Server]
        public void ServerTriggerHit(HandSide side)
        {
            RpcTriggerHit(side);
        }

        [ObserversRpc]
        private void RpcTriggerHit(HandSide side)
        {
            _animator.SetTrigger(side == HandSide.Left ? Animations.Humanoid.HitTriggerLeft : Animations.Humanoid.HitTriggerRight);
        }

        private int GetFlinchParameter(FlinchRegion region)
        {
            return region switch
            {
                FlinchRegion.ArmLeft => Animations.Humanoid.HurtArmLeft,
                FlinchRegion.ArmRight => Animations.Humanoid.HurtArmRight,
                FlinchRegion.LegLeft => Animations.Humanoid.HurtLegLeft,
                FlinchRegion.LegRight => Animations.Humanoid.HurtLegRight,
                FlinchRegion.HeadFront => Animations.Humanoid.HurtHeadFront,
                FlinchRegion.HeadBack => Animations.Humanoid.HurtHeadBack,
                FlinchRegion.TorsoFront => Animations.Humanoid.HurtTorsoFront,
                FlinchRegion.TorsoBack => Animations.Humanoid.HurtTorsoBack,
                _ => throw new ArgumentOutOfRangeException(nameof(region), region, null),
            };
        }
    }
}
