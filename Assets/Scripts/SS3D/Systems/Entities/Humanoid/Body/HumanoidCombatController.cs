using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;
using NetworkConnection = FishNet.Connection.NetworkConnection;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Drives combat stance presentation and attack telegraphs (#1246).
    /// <c>F</c> toggles Help/Harm intent (combat mode follows Harm via <see cref="InteractionController"/>).
    /// Combat subtype (Melee vs Ranged) comes from inventory via <see cref="HumanoidBodyStateBridge"/>.
    /// Melee swing / ranged fire / reload telegraphs are requested via <see cref="RequestAttack"/>.
    /// </summary>
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    public class HumanoidCombatController : NetworkActor
    {
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private AnimationOrchestrator _orchestrator;
        [SerializeField] private HumanoidBodyStateBridge _bodyStateBridge;
        [SerializeField] private InteractionController _interactionController;

        private InputAction _toggleIntentAction;
        private bool _intentSubscribed;

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

            if (_interactionController == null)
            {
                _interactionController = GetComponent<InteractionController>();
            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (IsOwner)
            {
                SubscribeToggleIntent();
            }
            else
            {
                UnsubscribeToggleIntent();
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            if (IsOwner)
            {
                SubscribeToggleIntent();
            }
        }

        protected override void OnDisabled()
        {
            UnsubscribeToggleIntent();
            base.OnDisabled();
        }

        private void SubscribeToggleIntent()
        {
            if (_intentSubscribed)
            {
                return;
            }

            if (!SubSystems.TryGet(out InputSubSystem input) || input == null)
            {
                return;
            }

            _toggleIntentAction = input.ToggleIntent;
            _toggleIntentAction.performed += HandleToggleIntent;
            _intentSubscribed = true;
        }

        private void UnsubscribeToggleIntent()
        {
            if (!_intentSubscribed || _toggleIntentAction == null)
            {
                _intentSubscribed = false;
                _toggleIntentAction = null;
                return;
            }

            _toggleIntentAction.performed -= HandleToggleIntent;
            _toggleIntentAction = null;
            _intentSubscribed = false;
        }

        private void HandleToggleIntent(InputAction.CallbackContext context)
        {
            if (!IsOwner || InputInterface.IsCapturingText)
            {
                return;
            }

            _interactionController?.RequestToggleIntent();
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

        /// <param name="windupSeconds">
        /// Effective melee windup (exertion-scaled) so the swing's strike frame lands at connect; 0 plays at natural speed.
        /// </param>
        public void RequestAttack(AnimationTriggerId attackType, float windupSeconds = 0f)
        {
            if (!_bodyStateMachine.CanPerformAction())
            {
                return;
            }

            byte windup = MeleeSwingAnimation.QuantizeWindup(windupSeconds);

            // Play immediately on the owning client — don't wait for ServerRpc + SyncVar.
            byte variant = _orchestrator != null
                ? _orchestrator.PlayAttackTrigger(attackType, windup)
                : (byte)0;
            _bodyStateMachine.CmdFireTrigger(attackType, variant, windup);
        }
    }
}
