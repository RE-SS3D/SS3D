using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Toggles combat/peaceful mode and drives strafe + aim behaviour (#1246).
    /// </summary>
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    public class HumanoidCombatController : NetworkActor
    {
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private AnimationOrchestrator _orchestrator;

        protected override void OnAwake()
        {
            base.OnAwake();
            if (_bodyStateMachine == null)
            {
                _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            }

            if (_orchestrator == null)
            {
                _orchestrator = GetComponent<AnimationOrchestrator>();
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            AddHandle(Coimbra.Services.PlayerLoopEvents.UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandleUpdate(ref Coimbra.Services.Events.EventContext context, in Coimbra.Services.PlayerLoopEvents.UpdateEvent updateEvent)
        {
            if (!IsOwner)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                HumanoidCombatMode newMode = _bodyStateMachine.CombatMode == HumanoidCombatMode.Combat
                    ? HumanoidCombatMode.Peaceful
                    : HumanoidCombatMode.Combat;
                _bodyStateMachine.CmdSetCombatMode(newMode);
            }
        }

        /// <summary>
        /// Called by combat system when a hit lands on this humanoid.
        /// </summary>
        public void OnHitReceived(Vector3 knockbackDirection, float knockbackForce, float staggerDuration)
        {
            if (!IsServer)
            {
                return;
            }

            _bodyStateMachine.ApplyStagger(staggerDuration);
            if (knockbackForce > 0f)
            {
                _bodyStateMachine.ApplyKnockback(knockbackDirection, knockbackForce);
            }
        }

        public void RequestAttack(AnimationTriggerId attackType)
        {
            if (!_bodyStateMachine.CanPerformAction())
            {
                return;
            }

            _bodyStateMachine.CmdFireTrigger(attackType);
        }
    }
}
