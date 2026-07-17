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
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private HumanoidLivingController _livingController;
        [SerializeField] private HumanHealthController _healthController;
        [SerializeField] private Hands _hands;

        private void Awake()
        {
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
            UpdateDragging();
        }

        /// <summary>
        /// Limp/drag writes keep feeding the animator via BodyStateMachine even while Ragdoll has
        /// disabled AnimationOrchestrator — that re-poses a walk cycle on a collapsed body.
        /// </summary>
        private bool ShouldSuppressBodyPresentation()
        {
            if (_healthController != null)
            {
                HealthSnapshot snapshot = _healthController.Snapshot;
                if (!snapshot.IsConscious
                    || snapshot.IsCardiacArrest
                    || snapshot.State == HealthState.Dead)
                {
                    return true;
                }
            }

            return TryGetComponent(out Ragdoll ragdoll) && ragdoll.IsKnockedDown;
        }

        private void HandleItemChanged(object sender, Item item)
        {
            UpdateArmHold();
            RefreshCombatStanceFromInventory();
        }

        /// <summary>
        /// Melee vs Ranged from the active hand item. Unarmed or non-ranged weapon → Melee.
        /// </summary>
        public HumanoidCombatMode ResolveCombatStance()
        {
            if (_hands == null)
            {
                return HumanoidCombatMode.Melee;
            }

            Item item = _hands.SelectedHand?.ItemInHand;
            if (item == null)
            {
                return HumanoidCombatMode.Melee;
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

            Hand activeHand = _hands.SelectedHand;
            Item item = activeHand?.ItemInHand;
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

            LimpSide side = LimpSide.None;
            if (leftDamage > 0.3f && leftDamage > rightDamage)
            {
                side = LimpSide.Left;
            }
            else if (rightDamage > 0.3f)
            {
                side = LimpSide.Right;
            }

            _bodyStateMachine.SetLimpSide(side);
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
