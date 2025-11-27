using JetBrains.Annotations;
using NaughtyAttributes;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public class Holdable : AbstractHoldable
    {
        [SerializeField]
        private bool _canHoldTwoHand;

        [SerializeField]
        private HandHoldType _singleHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHold;

        [SerializeField]
        private HandHoldType _singleHandHoldHarm;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldHarm;

        [SerializeField]
        private HandHoldType _singleHandHoldThrow;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldThrow;

        [SerializeField]
        private FingerPoseType _primaryHandPoseType;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private FingerPoseType _secondaryHandPoseType;

        public override bool CanHoldTwoHand => _canHoldTwoHand;

        public override FingerPoseType PrimaryHandPoseType => _primaryHandPoseType;

        public override FingerPoseType SecondaryHandPoseType => _secondaryHandPoseType;

        public override HandHoldType SingleHandHold => _singleHandHold;

        public override HandHoldType TwoHandHold => _twoHandHold;

        public override HandHoldType SingleHandHoldHarm => _singleHandHoldHarm;

        public override HandHoldType TwoHandHoldHarm => _twoHandHoldHarm;
    }
}
