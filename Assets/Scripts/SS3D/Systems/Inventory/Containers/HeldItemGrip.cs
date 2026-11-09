using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// The rotation maths for holding an item at a display point. An item's <c>AttachmentPoint.localRotation</c>
    /// is the item root's rotation in character space while the rig is in the reference pose. A calibrated
    /// <see cref="GripSocket"/> turns that into a rotation local to the display, which then follows the hand.
    /// </summary>
    public static class HeldItemGrip
    {
        /// <summary>Rotation from the display to the character root, measured in the reference pose.</summary>
        public static Quaternion ReferenceRotation(Quaternion displayWorldRotation, Quaternion characterRootWorldRotation) =>
            Normalize(Quaternion.Inverse(displayWorldRotation) * characterRootWorldRotation);

        /// <summary>
        /// Grip pivot rotation local to a calibrated display. Constant: independent of the hand's current pose
        /// and of the character's facing. In the reference pose its world rotation equals
        /// <see cref="LegacyPivotWorldRotation"/>.
        /// </summary>
        public static Quaternion PivotLocalRotation(Quaternion referenceRotation, Quaternion attachmentLocalRotation) =>
            Normalize(referenceRotation * attachmentLocalRotation);

        /// <summary>
        /// The pre-socket grip: character facing × attachment, applied in world space at attach time. Its
        /// rotation relative to the hand depends on the hand's pose at that moment. Kept for uncalibrated
        /// displays only.
        /// </summary>
        public static Quaternion LegacyPivotWorldRotation(Quaternion characterRootWorldRotation, Quaternion attachmentLocalRotation) =>
            Normalize(characterRootWorldRotation * attachmentLocalRotation);

        private static Quaternion Normalize(Quaternion q)
        {
            float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (magnitude < Mathf.Epsilon)
            {
                return Quaternion.identity;
            }

            return new Quaternion(q.x / magnitude, q.y / magnitude, q.z / magnitude, q.w / magnitude);
        }
    }
}
