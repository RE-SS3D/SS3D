using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// The server's aim ray for a shot or swing (tech-debt P1.1; combat §3: line of sight is checked from the eye).
    /// The origin is the attacker's eye on the server's own copy of the body. The client sends only the point it aims
    /// at, so a modified client can pick a direction but never where the ray starts.
    /// </summary>
    public static class CombatAimRay
    {
        /// <summary>Eye height used when a body has no <c>ViewPoint</c>; matches the old fallback.</summary>
        public const float FallbackEyeHeight = 1.5f;

        /// <summary>
        /// A ray from <paramref name="serverEye"/> through <paramref name="clientAimPoint"/>. False when either is not
        /// finite or the aim point sits on the eye. An aim point past weapon range is kept: the resolver caps the ray at
        /// the weapon's range, so it only sets the direction.
        /// </summary>
        public static bool TryBuild(Vector3 serverEye, Vector3 clientAimPoint, out Ray aimRay)
        {
            aimRay = default;
            if (!RpcInput.IsFinite(serverEye) || !RpcInput.IsFinite(clientAimPoint))
            {
                return false;
            }

            Vector3 direction = clientAimPoint - serverEye;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            aimRay = new Ray(serverEye, direction.normalized);
            return true;
        }
    }
}
