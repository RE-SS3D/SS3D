using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Calibration for one <see cref="ContainerItemDisplay"/> display point (a hand, the torso). Stores the
    /// rotation from this display to the character root while the rig plays its reference pose (the
    /// Animator's default idle state). With it, a held item's grip is a constant hand-local rotation, so it
    /// no longer depends on which animation frame the hand was in when the item attached.
    /// Baked by <c>HandGripSocketCalibration</c> (SS3D → Inventory → Calibrate Hand Grip Sockets); see
    /// Documents/plans/item-orientation.plan.md Phase A.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GripSocket : MonoBehaviour
    {
        [Tooltip("Display-to-character-root rotation in the reference pose. Written by the calibration recipe.")]
        [SerializeField]
        private Quaternion _referenceRotation = Quaternion.identity;

        [Tooltip("False until the calibration recipe has run. Uncalibrated sockets use the legacy grip.")]
        [SerializeField]
        private bool _calibrated;

        [Tooltip("What pose the calibration was taken in, for the record.")]
        [SerializeField]
        private string _referencePose;

        public bool IsCalibrated => _calibrated;

        public Quaternion ReferenceRotation => _referenceRotation;

        public string ReferencePose => _referencePose;

        /// <summary>Record the calibration. Called by the editor recipe with the rig in its reference pose.</summary>
        public void Calibrate(Quaternion displayWorldRotation, Quaternion characterRootWorldRotation, string referencePose)
        {
            _referenceRotation = HeldItemGrip.ReferenceRotation(displayWorldRotation, characterRootWorldRotation);
            _referencePose = referencePose;
            _calibrated = true;
        }
    }
}
