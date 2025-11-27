using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Gives access to all necessary data for hands IK to function properly, associated to a given hand.
    /// </summary>
    public class HandHoldIk : NetworkActor, IInteractiveTool
    {
        // The upper arm connecting this hand to the torso.
        [field: SerializeField]
        public Transform UpperArm { get; private set; }

        /// <summary>
        /// The IK target for the chain IK constraint, allowing the player to bend to reach things.
        /// </summary>
        [field: SerializeField]
        public Transform HandIkTarget { get; private set; }

        /// <summary>
        /// The IK target for the item position when held by player, items are going to be parented on it.
        /// </summary>
        [field: SerializeField]
        public Transform ItemPositionTargetLocker { get; private set; }

        /// <summary>
        /// Transform used by rifles when aiming, used by the weapon aim rig.
        /// </summary>
        [field: SerializeField]
        public Transform ShoulderWeaponPivot { get; private set; }

        /// <summary>
        /// The two bone IK constraint allowing the IK hold poses.
        /// </summary>
        [field: SerializeField]
        public TwoBoneIKConstraint HoldIkConstraint { get; private set; }

        /// <summary>
        /// The chain IK constraint, allowing the player to bend to reach things.
        /// </summary>
        [field: SerializeField]
        public ChainIKConstraint PickupIkConstraint { get; private set; }

        /// <summary>
        /// The MultiPositionConstraint constraint, allowing to place items on given position and moves them following the shoulder.
        /// This works a bit like transform parenting.
        /// </summary>
        [field: SerializeField]
        public MultiPositionConstraint ItemPositionConstraint { get; private set; }

        /// <summary>
        /// The transform for the hand bone, used as the tip in the two bones IK constraint
        /// </summary>
        [field: SerializeField]
        public Transform HandBone { get; private set; }

        /// <summary>
        /// The transform for the hand hold point, its where you'd put item in hand if you parented them to hand.
        /// </summary>
        [field: SerializeField]
        public Transform HoldTransform { get; private set; }

        [field: SerializeField]
        public Transform Pivot { get; private set; }

        [field: SerializeField]
        public Hands Hands { get; private set; }

        /// <summary>
        /// The hand the IK data in this class is for.
        /// </summary>
        [field: SerializeField]
        public Hand Hand { get; private set; }

        [field: SerializeField]
        public Transform InteractionPoint { get; private set; }

        /// <summary>
        /// The IK target for the chain IK constraint, allowing the player to bend to reach things.
        /// </summary>
        [field: SerializeField]
        public TargetFollow HandTargetFollow { get; private set; }

        public void HandTargetFollowHold(bool secondary, AbstractHoldable holdProvider, bool simulateRotation = true, float timeToReachRotation = 0f, bool update = true)
        {
            Transform parent = holdProvider.GetHold(!secondary, Hand.HandType);
            HandTargetFollowTransform(parent, simulateRotation, timeToReachRotation);
        }

        /// <summary>
        /// Put the right IK target as a child of another transform, setting its position to zero and its rotation to be the same as the parent one (or optionnally not doing it).
        /// </summary>
        public void HandTargetFollowTransform(Transform parent, bool simulateRotation = true, float timeToReachRotation = 0f, bool update = true)
        {
            HandTargetFollow.Follow(parent, simulateRotation, timeToReachRotation);
        }

        /// <summary>
        /// Remove the hold ik constraint weight on an item and unparent it. (Move that to Hold controller ?)
        /// </summary>
        /// <param name="item"></param>
        public void StopHolding(Item item)
        {
            item.transform.parent = null;
            HoldIkConstraint.weight = 0f;
            Hands.TryGetOppositeHand(Hand, out Hand oppositeHand);
            bool withTwoHands = oppositeHand.Empty && item.Holdable.CanHoldTwoHand;

            if (withTwoHands)
            {
                oppositeHand.Hold.HoldIkConstraint.weight = 0f;
            }
        }

        /// <summary>
        /// Play a procedural animation for the hand depending on the interaction type.
        /// </summary>
        /// <param name="interactionType"></param>
        public void PlayAnimation(InteractionType interactionType)
        {
            switch (interactionType)
            {
                case InteractionType.Open:
                {
                    PlayOpenAnimation();
                    break;
                }
            }
        }

        public void StopAnimation()
        {
        }

        /// <summary>
        /// have the hand do kind of an opening move (for opening toolboxes and whatnot).
        /// </summary>
        private void PlayOpenAnimation()
        {
            // Define the points for the parabola
            Vector3[] path = new Vector3[]
            {
                HandBone.position,                             // Starting position
                HandBone.position + (HandBone.forward * 0.1f), // Peak (the highest point)
                HandBone.position + (HandBone.up * 0.1f),      // Final position
            };

            // Animate the GameObject along the path in a smooth parabolic motion
            HandIkTarget.DOPath(path, 0.1f, PathType.CatmullRom)
                .SetEase(Ease.Linear) // You can adjust the ease function as needed
                .SetLoops(1, LoopType.Restart); // Play the animation once, no loops
        }
    }
}
