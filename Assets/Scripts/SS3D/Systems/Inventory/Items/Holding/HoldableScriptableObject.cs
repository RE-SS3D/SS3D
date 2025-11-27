using NaughtyAttributes;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    [CreateAssetMenu(fileName = "HoldableScriptableObject", menuName = "Holdable/HoldableScriptableObject", order = 0)]
    public class HoldableScriptableObject : ScriptableObject
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
        private FingerPoseType _primaryHandPoseType;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private FingerPoseType _secondaryHandPoseType;

        public bool CanHoldTwoHand => _canHoldTwoHand;

        public FingerPoseType PrimaryHandPoseType => _primaryHandPoseType;

        public FingerPoseType SecondaryHandPoseType => _secondaryHandPoseType;

        public HandHoldType SingleHandHold => _singleHandHold;

        public HandHoldType TwoHandHold => _twoHandHold;

        public HandHoldType SingleHandHoldHarm => _singleHandHoldHarm;

        public HandHoldType TwoHandHoldHarm => _twoHandHoldHarm;
    }
}
