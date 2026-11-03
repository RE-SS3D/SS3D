using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Bridges health, inventory, and drag systems into the body state machine.
    /// </summary>
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    public class HumanoidBodyStateBridge : MonoBehaviour
    {
        private const float LimpThreshold = 0.3f;
        private const float WaveEmoteLegThreshold = 0.75f;
        private const float WaveEmoteCooldownSeconds = 48f;

        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private HumanoidLivingController _livingController;
        [SerializeField] private HumanHealthController _healthController;
        [SerializeField] private Hands _hands;

        private float _nextHurtEmoteTime;

        private void Awake()
        {
            _nextHurtEmoteTime = Time.time + WaveEmoteCooldownSeconds * 0.5f;

            if (_bodyStateMachine == null)
            {
                _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            }

            if (_livingController == null)
            {
                _livingController = GetComponent<HumanoidLivingController>();
            }

            if (_healthController == null)
            {
                _healthController = GetComponent<HumanHealthController>();
            }

            if (_hands == null)
            {
                _hands = GetComponent<Hands>();
            }
        }

        private void Start()
        {
            Subscribe();
            UpdateArmHold();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_hands != null)
            {
                foreach (Hand hand in _hands.PlayerHands)
                {
                    hand.Container.OnItemAttached += HandleItemChanged;
                    hand.Container.OnItemDetached += HandleItemChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            if (_hands != null)
            {
                foreach (Hand hand in _hands.PlayerHands)
                {
                    hand.Container.OnItemAttached -= HandleItemChanged;
                    hand.Container.OnItemDetached -= HandleItemChanged;
                }
            }
        }

        private void Update()
        {
            if (ShouldSuppressBodyPresentation())
            {
                return;
            }

            UpdateLimp();
            UpdateInjuredArms();
            UpdateMirrorUpperBody();
            UpdateDragging();
        }

        /// <summary>
        /// Limp/drag writes keep feeding the animator via BodyStateMachine even while Ragdoll has
        /// disabled AnimationOrchestrator — that re-poses a walk cycle on a collapsed body.
        /// Presentation is owned by <see cref="Ragdoll"/>; do not re-derive from Health here.
        /// </summary>
        private bool ShouldSuppressBodyPresentation()
        {
            return TryGetComponent(out Ragdoll ragdoll)
                && ragdoll.Presentation != BodyPresentationState.Locomotion;
        }

        private void HandleItemChanged(object sender, Item item)
        {
            UpdateArmHold();
            RefreshCombatStanceFromInventory();
        }

        /// <summary>
        /// Melee vs Ranged from the wielded combat item. Prefers a two-hand rifle in its required
        /// hand (even when the off-hand is selected for UI); falls back to selected-hand item /
        /// ranged trait name match for unwired content.
        /// </summary>
        public HumanoidCombatMode ResolveCombatStance()
        {
            if (_hands == null)
            {
                return HumanoidCombatMode.Melee;
            }

            Item item = TwoHandedWeaponRules.ResolvePresentationItem(_hands);
            if (item == null)
            {
                return HumanoidCombatMode.Melee;
            }

            if (item.TryGetComponent(out RangedWeaponItemExtension _))
            {
                return HumanoidCombatMode.Ranged;
            }

            foreach (Trait trait in item.Traits)
            {
                if (trait == null || string.IsNullOrEmpty(trait.Name))
                {
                    continue;
                }

                string name = trait.Name;
                if (name.Contains("Ranged", System.StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Gun", System.StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Firearm", System.StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Rifle", System.StringComparison.OrdinalIgnoreCase))
                {
                    return HumanoidCombatMode.Ranged;
                }
            }

            return HumanoidCombatMode.Melee;
        }

        private void RefreshCombatStanceFromInventory()
        {
            if (_bodyStateMachine == null || !_bodyStateMachine.CombatMode.IsCombat())
            {
                return;
            }

            // ServerRpc — only the owning client should request a stance change.
            if (!_bodyStateMachine.IsOwner)
            {
                return;
            }

            HumanoidCombatMode stance = ResolveCombatStance();
            if (stance != _bodyStateMachine.CombatMode)
            {
                _bodyStateMachine.CmdSetCombatMode(stance);
            }
        }

        private void UpdateArmHold()
        {
            if (_hands == null || _bodyStateMachine == null)
            {
                return;
            }

            Item item = TwoHandedWeaponRules.ResolvePresentationItem(_hands);
            ArmHoldPose pose = ArmHoldPose.Default;
            if (item != null)
            {
                foreach (Trait trait in item.Traits)
                {
                    if (trait != null && trait.Name.Contains("Weapon", System.StringComparison.OrdinalIgnoreCase))
                    {
                        pose = ArmHoldPose.Weapon;
                        break;
                    }
                }
                if (pose == ArmHoldPose.Default)
                {
                    pose = ArmHoldPose.Item;
                }
            }

            _bodyStateMachine.SetArmHold(pose);
        }

        private void UpdateMirrorUpperBody()
        {
            if (_bodyStateMachine == null || _hands == null)
            {
                return;
            }

            _bodyStateMachine.SetMirrorUpperBody(TwoHandedWeaponRules.ShouldMirrorUpperBody(_hands));
        }

        private void UpdateLimp()
        {
            if (_bodyStateMachine == null)
            {
                return;
            }

            // Foot/leg injury slows and limps the gait. The legacy FeetController/FootBodyPart
            // path was removed in the health rewrite; per-leg damage now comes from the zone model.
            float leftDamage = _healthController != null ? _healthController.GetZoneBruteFraction(BodyZone.LeftLeg) : 0f;
            float rightDamage = _healthController != null ? _healthController.GetZoneBruteFraction(BodyZone.RightLeg) : 0f;
            float maxLeg = Mathf.Max(leftDamage, rightDamage);

            LimpSide side = LimpSide.None;
            if (leftDamage > LimpThreshold && leftDamage > rightDamage)
            {
                side = LimpSide.Left;
            }
            else if (rightDamage > LimpThreshold)
            {
                side = LimpSide.Right;
            }

            _bodyStateMachine.SetLimpSide(side);
            _bodyStateMachine.SetInjuredLeg(maxLeg);
            TickRareHurtEmote(maxLeg);
        }

        /// <summary>
        /// While severely limping and nearly idle, occasionally fire Emote (Injured Wave on animator).
        /// </summary>
        private void TickRareHurtEmote(float maxLeg)
        {
            if (_bodyStateMachine == null || maxLeg < WaveEmoteLegThreshold)
            {
                return;
            }

            if (_bodyStateMachine.Snapshot.MovementSpeed > 0.15f)
            {
                return;
            }

            if (Time.time < _nextHurtEmoteTime)
            {
                return;
            }

            _nextHurtEmoteTime = Time.time + WaveEmoteCooldownSeconds;
            // Owner prediction + server auth via existing trigger RPC path.
            if (_bodyStateMachine.IsOwner)
            {
                _bodyStateMachine.CmdFireTrigger(AnimationTriggerId.Emote, 0, 0);
            }
        }

        private void UpdateInjuredArms()
        {
            if (_bodyStateMachine == null)
            {
                return;
            }

            float left = _healthController != null ? _healthController.GetZoneBruteFraction(BodyZone.LeftArm) : 0f;
            float right = _healthController != null ? _healthController.GetZoneBruteFraction(BodyZone.RightArm) : 0f;
            _bodyStateMachine.SetInjuredArms(left, right);
        }

        private void UpdateDragging()
        {
            if (_livingController != null && _bodyStateMachine != null)
            {
                _bodyStateMachine.SetDragging(_livingController.IsDragging);
            }
        }
    }
}
