using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Toggles peaceful / combat stance and drives strafe + aim behaviour (#1246).
    /// Combat subtype (Melee vs Ranged) comes from inventory via <see cref="HumanoidBodyStateBridge"/>.
    /// Melee LMB swing is triggered from <see cref="SS3D.Systems.Interactions.InteractionController"/>
    /// (Run Primary) so it shares the same input path as interactions.
    /// </summary>
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    public class HumanoidCombatController : NetworkActor
    {
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private AnimationOrchestrator _orchestrator;
        [SerializeField] private HumanoidBodyStateBridge _bodyStateBridge;

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

            if (_bodyStateBridge == null)
            {
                _bodyStateBridge = GetComponent<HumanoidBodyStateBridge>();
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
                if (_bodyStateMachine.CombatMode.IsCombat())
                {
                    _bodyStateMachine.CmdSetCombatMode(HumanoidCombatMode.Peaceful);
                }
                else
                {
                    HumanoidCombatMode stance = _bodyStateBridge != null
                        ? _bodyStateBridge.ResolveCombatStance()
                        : HumanoidCombatMode.Melee;
                    _bodyStateMachine.CmdSetCombatMode(stance);
                }
            }
        }

        /// <summary>
        /// Called from interaction Run Primary while in melee combat.
        /// Returns true if the click was consumed as an attack.
        /// </summary>
        public bool TryHandlePrimaryAttack()
        {
            if (!IsOwner || _bodyStateMachine == null)
            {
                return false;
            }

            if (_bodyStateMachine.CombatMode != HumanoidCombatMode.Melee)
            {
                return false;
            }

            RequestAttack(AnimationTriggerId.AttackSwing);
            return true;
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

            // Play immediately on the owning client — don't wait for ServerRpc + SyncVar.
            _orchestrator?.PlayAttackTrigger(attackType);
            _bodyStateMachine.CmdFireTrigger(attackType);
        }
    }
}
