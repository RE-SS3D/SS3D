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
        [SerializeField] private FeetController _feetController;
        [SerializeField] private Hands _hands;

        private FootBodyPart _leftFoot;
        private FootBodyPart _rightFoot;

        private void Awake()
        {
            _bodyStateMachine ??= GetComponent<HumanoidBodyStateMachine>();
            _livingController ??= GetComponent<HumanoidLivingController>();
            _feetController ??= GetComponent<FeetController>();
            _hands ??= GetComponent<Hands>();
        }

        private void Start()
        {
            CacheFeet();
            Subscribe();
            UpdateArmHold();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void CacheFeet()
        {
            FootBodyPart[] feet = GetComponentsInChildren<FootBodyPart>();
            foreach (FootBodyPart foot in feet)
            {
                if (foot.name.Contains("Left", System.StringComparison.OrdinalIgnoreCase))
                {
                    _leftFoot = foot;
                }
                else if (foot.name.Contains("Right", System.StringComparison.OrdinalIgnoreCase))
                {
                    _rightFoot = foot;
                }
            }
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
            UpdateLimp();
            UpdateDragging();
        }

        private void HandleItemChanged(object sender, Item item)
        {
            UpdateArmHold();
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
                foreach (SS3D.Systems.Trait trait in item.Traits)
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

            float leftDamage = _leftFoot != null ? _leftFoot.RelativeDamage : 0f;
            float rightDamage = _rightFoot != null ? _rightFoot.RelativeDamage : 0f;

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
