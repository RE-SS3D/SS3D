using System;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Finds a working <see cref="BreakableDevice"/> along a shot or swing.</summary>
    public static class BreakableDeviceResolver
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>
        /// The nearest working device whose collider <paramref name="ray"/> enters within
        /// <paramref name="maxDistance"/>. Wall mounts sit inside their wall's full-tile collider, so callers
        /// pass the wall hit plus <see cref="SS3D.Systems.Electricity.LightFixtureBulb.WallInset"/>.
        /// </summary>
        public static bool TryFind(Ray ray, float maxDistance, out BreakableDevice device, out RaycastHit hit)
        {
            device = null;
            hit = default;
            int count = Physics.RaycastNonAlloc(ray, Hits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(Hits, 0, count, HitDistance.Instance);
            for (int i = 0; i < count; i++)
            {
                BreakableDevice candidate = Hits[i].collider.GetComponentInParent<BreakableDevice>();
                if (candidate != null && !candidate.IsBroken)
                {
                    device = candidate;
                    hit = Hits[i];
                    return true;
                }
            }

            return false;
        }

        private sealed class HitDistance : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistance Instance = new();

            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
