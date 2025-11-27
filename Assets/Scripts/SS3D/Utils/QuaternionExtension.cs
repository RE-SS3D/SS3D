using UnityEngine;

namespace SS3D.Utils
{
    public static class QuaternionExtension
    {
        /// <summary>
        /// Create a LookRotation for a non-standard 'forward' axis.
        /// </summary>
        public static Quaternion AltForwardLookRotation(Vector3 dir, Vector3 forwardAxis, Vector3 upAxis)
        {
            return Quaternion.LookRotation(dir) * Quaternion.Inverse(Quaternion.LookRotation(forwardAxis, upAxis));
        }

        /// <summary>
        ///  Same as look rotation, but the direction is always orthogonal to the world up vector.
        /// </summary>
        public static Quaternion SameHeightPlaneLookRotation(Vector3 dir, Vector3 upAxis)
        {
            dir.y = 0f;

            return Quaternion.LookRotation(dir, upAxis);
        }
    }
}
