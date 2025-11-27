using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Handle the state of holding items and hand positions
    /// </summary>
    public class HoldController : NetworkActor
    {
        private readonly Dictionary<HandHoldType, string> _rightHandHoldTypeToAnimatorPoseParameter = new();

        private readonly Dictionary<HandHoldType, string> _leftHandHoldTypeToAnimatorPoseParameter = new();

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private AimController _aimController;

        private string _rightHandCurrentPose = "ArmIdlePoseRight";

        private string _leftHandCurrentPose = "ArmIdlePoseLeft";

        public override void OnStartServer()
        {
            base.OnStartServer();

            foreach (Hand hand in GetComponentsInChildren<Hand>())
            {
                hand.OnContentsChanged += HandleHandContentChanged;
            }

            GetComponent<Ragdoll>().OnRagdoll += HandleRagdoll;
        }

        public void BringToHand(Hand hand, AbstractHoldable holdable, float duration)
        {
            StartCoroutine(CoroutineBringToHand(hand, holdable, duration));
        }

        protected override void OnAwake()
        {
            _intents.OnIntentChange += HandleIntentChange;
            _aimController.OnAim += HandleAimChange;

            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] = "ArmIdlePoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandRifle] = "RifleRestPoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.UnderArm] = "UnderArmPoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandRifleHarm] = "RifleHarmPoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.SingleHandRifle] = "SingleHandRiflePoseRight";

            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] = "ArmIdlePoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandRifle] = "RifleRestPoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.UnderArm] = "UnderArmPoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandRifleHarm] = "RifleHarmPoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.SingleHandRifle] = "SingleHandRiflePoseLeft";
        }

        private IEnumerator CoroutineBringToHand([NotNull] Hand hand, [NotNull] AbstractHoldable holdable, float duration)
        {
            holdable.GetComponent<Item>().SetFreeze(true);
            Transform holdOnItem = holdable.GetHold(true, hand.HandType);
            Vector3 startPosition = holdable.transform.position;
            Quaternion startRotation = holdable.transform.rotation;

            // multiply by inverse of hold transform to "remove" the hold parent rotation.
            hand.Hold.Pivot.transform.localRotation = Quaternion.Inverse(hand.Hold.HoldTransform.localRotation) * Quaternion.Inverse(holdOnItem.localRotation);

            // Smoothly move item toward the target position
            for (float timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
            {
                float factor = timePassed / duration;

                holdable.transform.position = Vector3.Lerp(startPosition, hand.Hold.Pivot.transform.position, factor);
                holdable.transform.localRotation = Quaternion.Lerp(startRotation,  hand.Hold.Pivot.transform.rotation, factor);

                yield return null;
            }

            holdable.transform.SetParent(hand.Hold.Pivot.transform, true);

            // Assign the relative position between the attachment point and the object
            holdable.transform.localPosition = -holdOnItem.localPosition;
            holdable.transform.localRotation = Quaternion.identity;
        }

        private void UpdatePoseWithItem([NotNull] Hand hand, [NotNull] AbstractHoldable item, float duration)
        {
            hand.Hold.HoldIkConstraint.weight = 0f;
            bool isRight = hand.HandType == HandType.RightHand;

            bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && item.CanHoldTwoHand && oppositeHand.Empty;

            // Fetch how the item should be held
            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent);

            float layerWeight = 1f;

            // If small item, depending on the item mass, set the arm layer weight, to allow for more or less arm movement.
            if (item.TryGetComponent(out Rigidbody itemRigidBody) && itemHoldType == HandHoldType.SmallItem)
            {
                layerWeight = Mathf.Min(0.3f + (itemRigidBody.mass / 10), 1f);
            }

            _animator.SetLayerWeight(_animator.GetLayerIndex(isRight ? "ArmRight" : "ArmLeft"), layerWeight);

            string currentPose = isRight ? _rightHandCurrentPose : _leftHandCurrentPose;
            string targetPose = isRight ? _rightHandHoldTypeToAnimatorPoseParameter[itemHoldType] : _leftHandHoldTypeToAnimatorPoseParameter[itemHoldType];

            StartCoroutine(SetPoseAnimation(currentPose, targetPose, isRight, duration));
        }

        private void UpdatePoseWithoutItem([NotNull] Hand hand, float duration)
        {
            bool isRight = hand.HandType == HandType.RightHand;
            _animator.SetLayerWeight(_animator.GetLayerIndex(isRight ? "ArmRight" : "ArmLeft"), 0);
            string currentPose = isRight ? _rightHandCurrentPose : _leftHandCurrentPose;
            string targetPose = isRight ? _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] : _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem];
            StartCoroutine(SetPoseAnimation(currentPose, targetPose, isRight, duration));
        }

        private IEnumerator SetPoseAnimation(string currentPose, string targetPose, bool isRight, float duration)
        {
            if (currentPose == targetPose)
            {
                yield break;
            }

            for (float timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
            {
                _animator.SetFloat(currentPose, 1 - (timePassed / duration));
                _animator.SetFloat(targetPose, timePassed / duration);
                yield return null;
            }

            _animator.SetFloat(currentPose, 0f);
            _animator.SetFloat(targetPose, 1f);

            if (isRight)
            {
                _rightHandCurrentPose = targetPose;
            }
            else
            {
                _leftHandCurrentPose = targetPose;
            }
        }

        [ObserversRpc(BufferLast = true)]
        private void RemoveItem(Hand hand)
        {
            UpdatePoseWithoutItem(hand, 0.2f);

            // if the other hand hold an item that can be held with two hands, do so.
            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full && oppositeHand.ItemHeld.Holdable.CanHoldTwoHand)
            {
                UpdatePoseWithItem(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f);
                hand.Hold.HoldIkConstraint.weight = 1f;
                hand.Hold.HandTargetFollowHold(true, oppositeHand.ItemHeld.Holdable);
            }
        }

        [ObserversRpc(BufferLast = true)]
        private void AddItem([NotNull] Hand hand, [NotNull] AbstractHoldable holdable)
        {
            BringToHand(hand, holdable, 0.2f);
            UpdatePoseWithItem(hand, holdable, 0.2f);

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full)
            {
                UpdatePoseWithItem(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f);
            }
        }

        [Server]
        private void HandleHandContentChanged(Hand hand, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (type == ContainerChangeType.Add)
            {
                AddItem(hand, newItem.Holdable);
            }
            else if (type == ContainerChangeType.Remove)
            {
                RemoveItem(hand);
            }
        }

        private void HandleIntentChange(object sender, IntentType e)
        {
            foreach (Hand hand in _hands.PlayerHands.Where(x => x.Full))
            {
                UpdatePoseWithItem(hand, hand.ItemHeld.Holdable, 0.25f);
            }
        }

        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            if (!_hands.SelectedHand.ItemHeld)
            {
                return;
            }

            AbstractHoldable holdable = _hands.SelectedHand.ItemHeld.Holdable;
            Gun gun = null;

            // handle aiming with shoulder aim
            if (holdable && holdable.TryGetComponent(out gun) && !toThrow && isAiming)
            {
                // when aiming, the main hand holding the gun should use IK, as the gun moves independently from the character armature.
                _hands.SelectedHand.Hold.HoldIkConstraint.weight = 1f;
                gun.transform.parent = _hands.SelectedHand.Hold.ShoulderWeaponPivot;

                // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
                gun.transform.localPosition = -gun.RifleButt.localPosition;
                gun.transform.localRotation = Quaternion.identity;
            }

            // Stop aiming with shoulder aim
            else if (gun && !isAiming)
            {
                BringToHand(_hands.SelectedHand, holdable, 0.2f);
                UpdatePoseWithItem(_hands.SelectedHand, holdable, 0.2f);
            }

            // if it's not a gun, or if its to throw it
            else
            {
                UpdatePoseWithItem(_hands.SelectedHand, holdable, 0.2f);
            }
        }

        [Server]
        private void HandleRagdoll(bool isRagdoll)
        {
            if (!isRagdoll)
            {
                return;
            }

            foreach (Hand hand in GetComponentsInChildren<Hand>())
            {
                hand.DropHeldItem();
            }
        }
    }
}
