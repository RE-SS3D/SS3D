using System;
using Coimbra;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Client-local ranged shot outcome: cross-flash on damaging hits, plus optional debug world markers.
    /// Gold/grey spheres are off by default — enable with <c>rangedmarkers on</c>.
    /// </summary>
    public static class RangedShotFeedback
    {
        private const float MarkerSeconds = 0.55f;
        private const float MarkerScaleWhiff = 0.08f;
        private const float MarkerScaleHit = 0.16f;
        /// <summary>
        /// Zone colliders sit inside the body mesh — pull the marker toward the shooter so it isn't buried.
        /// </summary>
        private const float PullTowardShooterHit = 0.28f;
        private const float PullTowardShooterWhiff = 0.06f;

        /// <summary>
        /// When true, spawn gold/grey impact spheres. Default off — diegetic feedback is bullet holes + muzzle flash.
        /// Toggled by the <c>rangedmarkers</c> console command.
        /// </summary>
        public static bool ShowImpactMarkers { get; set; }

        public static event Action<Vector3, bool> LocalShotImpact;

        public static void NotifyLocalShotImpact(Vector3 worldPoint, bool damagingHit, Vector3 shotDirection)
        {
            Vector3 displayPoint = worldPoint;
            if (shotDirection.sqrMagnitude > 0.0001f)
            {
                float pull = damagingHit ? PullTowardShooterHit : PullTowardShooterWhiff;
                displayPoint = worldPoint - shotDirection.normalized * pull;
            }

            LocalShotImpact?.Invoke(displayPoint, damagingHit);
            if (damagingHit)
            {
                MeleeConnectFeedback.NotifyLocalConnectHitLanded();
            }

            if (ShowImpactMarkers)
            {
                SpawnImpactMarker(displayPoint, damagingHit);
            }
        }

        private static void SpawnImpactMarker(Vector3 worldPoint, bool damagingHit)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "RangedImpactMarker";
            marker.hideFlags = HideFlags.HideAndDontSave;
            marker.transform.position = worldPoint;
            float scale = damagingHit ? MarkerScaleHit : MarkerScaleWhiff;
            marker.transform.localScale = Vector3.one * scale;

            if (marker.TryGetComponent(out Collider collider))
            {
                collider.Dispose(true);
            }

            if (marker.TryGetComponent(out Renderer renderer))
            {
                // Unlit-ish via shared material tint; URP Lit still reads at station scale.
                Color color = damagingHit
                    ? new Color(1f, 0.85f, 0.2f, 1f)
                    : new Color(0.75f, 0.75f, 0.8f, 1f);
                renderer.material.color = color;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            TimedDispose lifetime = marker.AddComponent<TimedDispose>();
            lifetime.Seconds = MarkerSeconds;
        }

        private sealed class TimedDispose : MonoBehaviour
        {
            public float Seconds = MarkerSeconds;

            private float _elapsed;

            private void Update()
            {
                _elapsed += Time.unscaledDeltaTime;
                if (_elapsed < Seconds)
                {
                    return;
                }

                gameObject.Dispose(true);
            }
        }
    }
}
