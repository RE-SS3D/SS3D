using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Default script because laziness prevents me to go through all our items and set how they should be held properly.
    /// </summary>
    public class DefaultHoldable : AbstractHoldable
    {
        public override bool CanHoldTwoHand => false;

        public override FingerPoseType PrimaryHandPoseType => FingerPoseType.Relaxed;

        public override FingerPoseType SecondaryHandPoseType => FingerPoseType.Relaxed;

        public override HandHoldType SingleHandHold => HandHoldType.SmallItem;

        public override HandHoldType TwoHandHold => HandHoldType.SmallItem;

        public override HandHoldType SingleHandHoldHarm => HandHoldType.SmallItem;

        public override HandHoldType TwoHandHoldHarm => HandHoldType.SmallItem;
    }
}
