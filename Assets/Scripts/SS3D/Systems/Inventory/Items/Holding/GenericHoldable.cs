using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public class GenericHoldable : AbstractHoldable
    {
        [SerializeField]
        private HoldableScriptableObject _holdableScriptableObject;

        public override bool CanHoldTwoHand => _holdableScriptableObject != null && _holdableScriptableObject.CanHoldTwoHand;

        public override FingerPoseType PrimaryHandPoseType => _holdableScriptableObject.PrimaryHandPoseType;

        public override FingerPoseType SecondaryHandPoseType => _holdableScriptableObject.SecondaryHandPoseType;

        public override HandHoldType SingleHandHold => _holdableScriptableObject.SingleHandHold;

        public override HandHoldType TwoHandHold => _holdableScriptableObject.TwoHandHold;

        public override HandHoldType SingleHandHoldHarm => _holdableScriptableObject.SingleHandHoldHarm;

        public override HandHoldType TwoHandHoldHarm => _holdableScriptableObject.TwoHandHoldHarm;
    }
}
