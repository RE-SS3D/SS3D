using System.Collections.Generic;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>Bounded fuel deposits. Contacts stay in collider space so fuel follows its surface.</summary>
    public sealed class NapalmPatchSet
    {
        public const int Capacity = 128;
        public const int MaxBurning = 64;
        public const float BurnSeconds = 6f;
        public const float Radius = 0.65f;
        public const float BurnPerSecond = 6f;
        private readonly List<Patch> _patches = new();
        private int _nextId;
        public IReadOnlyList<Patch> Patches => _patches;

        public sealed class Patch
        {
            public int Id;
            public Collider Surface;
            public Vector3 LocalPoint;
            public Vector3 LocalNormal;
            public float Remaining;
            internal Vector3 LastPoint;
            internal Vector3 LastNormal;
            public Vector3 Point => Surface.transform.TransformPoint(LocalPoint);
            public Vector3 Normal => Surface.transform.TransformDirection(LocalNormal).normalized;
        }

        public void Deposit(Collider surface, Vector3 point, Vector3 normal)
        {
            if (surface == null) return;
            normal.Normalize();
            foreach (Patch patch in _patches)
            {
                if (patch.Surface == surface && Vector3.Dot(patch.Normal, normal) > 0.9f
                    && (patch.Point - point).sqrMagnitude < Radius * Radius)
                {
                    LimitBurning(patch);
                    patch.Remaining = BurnSeconds;
                    return;
                }
            }
            LimitBurning(null);
            if (_patches.Count >= Capacity)
            {
                int oldest = 0;
                for (int i = 1; i < _patches.Count; i++)
                    if (_patches[i].Remaining < _patches[oldest].Remaining) oldest = i;
                _patches.RemoveAt(oldest);
            }
            _patches.Add(new Patch
            {
                Id = ++_nextId, Surface = surface, Remaining = BurnSeconds,
                LocalPoint = surface.transform.InverseTransformPoint(point),
                LocalNormal = surface.transform.InverseTransformDirection(normal),
                LastPoint = point, LastNormal = normal,
            });
        }

        private void LimitBurning(Patch refreshed)
        {
            if (refreshed != null && refreshed.Remaining > 0f) return;
            int count = 0;
            Patch oldest = null;
            foreach (Patch patch in _patches)
            {
                if (patch.Remaining <= 0f) continue;
                count++;
                if (oldest == null || patch.Remaining < oldest.Remaining) oldest = patch;
            }
            if (count >= MaxBurning && oldest != null) oldest.Remaining = 0f;
        }

        /// <summary>Expired fuel retains a cold scorch record until eviction, surface removal or map reset.</summary>
        public bool Step(float seconds)
        {
            bool changed = false;
            for (int i = _patches.Count - 1; i >= 0; i--)
            {
                Patch patch = _patches[i];
                if (patch.Surface == null || !patch.Surface.enabled || !patch.Surface.gameObject.activeInHierarchy)
                {
                    _patches.RemoveAt(i);
                    changed = true;
                    continue;
                }
                bool wasBurning = patch.Remaining > 0f;
                patch.Remaining = Mathf.Max(0f, patch.Remaining - seconds);
                changed |= wasBurning && patch.Remaining <= 0f;
                changed |= (patch.Point - patch.LastPoint).sqrMagnitude > 0.000001f
                    || (patch.Normal - patch.LastNormal).sqrMagnitude > 0.000001f;
                patch.LastPoint = patch.Point;
                patch.LastNormal = patch.Normal;
            }
            return changed;
        }

        public void Clear() => _patches.Clear();
    }

    /// <summary>Compact, bounded world-state snapshot, including remaining lifetime for joining observers.</summary>
    public struct NapalmPatchSnapshot
    {
        public int Id;
        public Vector3 Point;
        public Vector3 Normal;
        public float Remaining;
        public bool IsBody;
        public int BodyObjectId;
        public BodyZone Zone;
    }
}
