using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Moves a wielded rifle's magazine into the left hand for the carry part of the reload clip,
    /// then seats it back on the gun. Presentation only: the magazine stays parented to the weapon
    /// and is posed each frame after animation, so nothing in the hierarchy or network changes.
    /// Driven by the Upper Body "Reload Rifle" state time, which plays on every observer.
    /// </summary>
    public sealed class ReloadMagazineHandoff
    {
        // Normalized clip windows; the moments themselves live in RifleReloadClipTiming.
        public static readonly float GrabStart = RifleReloadClipTiming.Normalized(RifleReloadClipTiming.GrabStartSeconds);
        public static readonly float GrabEnd = RifleReloadClipTiming.Normalized(RifleReloadClipTiming.GrabEndSeconds);
        public static readonly float InsertStart = RifleReloadClipTiming.Normalized(RifleReloadClipTiming.InsertStartSeconds);
        public static readonly float InsertEnd = RifleReloadClipTiming.Normalized(RifleReloadClipTiming.MagazineInSeconds);

        private Transform _magazine;
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;

        /// <summary>How much the magazine follows the left hand (0 = on the gun, 1 = in the hand).</summary>
        public static float HandWeight(float reloadNormalizedTime)
        {
            if (reloadNormalizedTime <= GrabStart || reloadNormalizedTime >= InsertEnd)
            {
                return 0f;
            }

            if (reloadNormalizedTime < GrabEnd)
            {
                return Mathf.SmoothStep(0f, 1f, (reloadNormalizedTime - GrabStart) / (GrabEnd - GrabStart));
            }

            if (reloadNormalizedTime <= InsertStart)
            {
                return 1f;
            }

            return Mathf.SmoothStep(1f, 0f, (reloadNormalizedTime - InsertStart) / (InsertEnd - InsertStart));
        }

        /// <summary>
        /// Poses the magazine for this frame. Call after every other bone write.
        /// <paramref name="reloadNormalizedTime"/> is negative when the reload state is not playing.
        /// </summary>
        public void Apply(RangedWeaponItemExtension weapon, Transform leftHand, float reloadNormalizedTime)
        {
            Transform magazine = weapon != null ? weapon.Magazine : null;
            if (magazine != _magazine)
            {
                Restore();
                _magazine = magazine;
                if (_magazine != null)
                {
                    _restLocalPosition = _magazine.localPosition;
                    _restLocalRotation = _magazine.localRotation;
                }
            }

            if (_magazine == null)
            {
                return;
            }

            // Start from the gun socket every frame so nothing accumulates.
            _magazine.SetLocalPositionAndRotation(_restLocalPosition, _restLocalRotation);
            float weight = leftHand != null && reloadNormalizedTime >= 0f ? HandWeight(reloadNormalizedTime) : 0f;
            if (weight <= 0f)
            {
                return;
            }

            Vector3 handPosition = leftHand.TransformPoint(weapon.MagazineHandPosition);
            Quaternion handRotation = leftHand.rotation * weapon.MagazineHandRotation;
            _magazine.SetPositionAndRotation(
                Vector3.Lerp(_magazine.position, handPosition, weight),
                Quaternion.Slerp(_magazine.rotation, handRotation, weight));
        }

        /// <summary>Puts the tracked magazine back on its gun and stops tracking it.</summary>
        public void Restore()
        {
            if (_magazine != null)
            {
                _magazine.SetLocalPositionAndRotation(_restLocalPosition, _restLocalRotation);
            }

            _magazine = null;
        }
    }
}
