using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>World-owned surface fuel, scorch records and burning bodies. No client-authoritative damage.</summary>
    public sealed class NapalmSubSystem : NetworkSubSystem
    {
        private const float TickSeconds = 0.25f;
        public const int MaxBurningBodies = 64;
        private readonly NapalmPatchSet _fuel = new();
        private readonly List<HumanHealthController> _bodies = new();
        private readonly List<ZoneTargetCollider> _limbs = new();
        private readonly List<BurningBody> _burning = new();
        private bool _dirty;
        private int _nextBodyId;
        private static SimulationChannel Channel => SimulationClock.GetOrCreate("napalm", TickSeconds, true);

        private sealed class BurningBody
        {
            public int Id;
            public HumanHealthController Health;
            public Collider Limb;
            public BodyZone Zone;
            public float Remaining;
            public float DamageCredit;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Channel.Subscribe(TickServer);
        }

        public override void OnStopServer()
        {
            Channel.Unsubscribe(TickServer);
            _fuel.Clear();
            _burning.Clear();
            _dirty = false;
            base.OnStopServer();
        }

        public override void OnStopClient()
        {
            NapalmSurfaceVfx.Clear();
            base.OnStopClient();
        }

        [Server]
        public void Deposit(RaycastHit hit)
        {
            _fuel.Deposit(hit.collider, hit.point, hit.normal);
            _dirty = true;
        }

        /// <summary>Spray contact and walking into a pool refresh one coating per body, never stacking DPS.</summary>
        [Server]
        public void Ignite(HumanHealthController health, BodyZone zone, Collider limb)
        {
            if (health == null || !health.IsServer || EntityInvincibility.IsInvincible(health)) return;
            foreach (BurningBody burning in _burning)
            {
                if (burning.Health != health) continue;
                burning.Remaining = NapalmPatchSet.BurnSeconds;
                _dirty = true;
                return;
            }
            if (_burning.Count >= MaxBurningBodies) _burning.RemoveAt(0);
            _burning.Add(new BurningBody
            {
                Id = --_nextBodyId, Health = health, Limb = limb, Zone = zone, Remaining = NapalmPatchSet.BurnSeconds,
            });
            _dirty = true;
        }

        /// <summary>Coats the body at the limb closest to a burning point (blowback onto the shooter).</summary>
        [Server]
        public void IgniteNearest(HumanHealthController health, Vector3 point)
        {
            if (health == null) return;
            _limbs.Clear();
            health.GetComponentsInChildren(true, _limbs);
            ZoneTargetCollider nearest = null;
            Collider nearestLimb = null;
            float best = float.MaxValue;
            foreach (ZoneTargetCollider zone in _limbs)
            {
                Collider limb = zone.GetComponent<Collider>();
                if (limb == null || !limb.enabled) continue;
                float distance = limb.bounds.SqrDistance(point);
                if (distance >= best) continue;
                best = distance;
                nearest = zone;
                nearestLimb = limb;
            }
            if (nearest != null) Ignite(health, nearest.Zone, nearestLimb);
        }

        [Server]
        public void ClearFuel()
        {
            _fuel.Clear();
            _burning.Clear();
            _dirty = false;
            ObserversSyncPatches(System.Array.Empty<NapalmPatchSnapshot>());
        }

        private void TickServer()
        {
            if (!IsServer) return;
            _dirty |= _fuel.Step(TickSeconds);
            bool surfaceBurning = false;
            foreach (NapalmPatchSet.Patch patch in _fuel.Patches)
                if (patch.Remaining > 0f) { surfaceBurning = true; break; }
            if (surfaceBurning)
            {
                SceneRegistry<HumanHealthController>.CopyTo(_bodies);
                foreach (HumanHealthController health in _bodies)
                {
                    if (health == null || !health.IsServer || EntityInvincibility.IsInvincible(health)) continue;
                    _limbs.Clear();
                    health.GetComponentsInChildren(true, _limbs);
                    foreach (NapalmPatchSet.Patch patch in _fuel.Patches)
                    {
                        if (patch.Remaining <= 0f) continue;
                        bool touching = false;
                        foreach (ZoneTargetCollider zone in _limbs)
                        {
                            Collider limb = zone.GetComponent<Collider>();
                            if (!NapalmSprayRules.Touches(patch.Point, patch.Normal, limb)) continue;
                            Ignite(health, zone.Zone, limb);
                            touching = true;
                            break;
                        }
                        if (touching) break;
                    }
                }
            }
            for (int i = _burning.Count - 1; i >= 0; i--)
            {
                BurningBody burning = _burning[i];
                if (burning.Health == null || !burning.Health.IsServer || !burning.Health.gameObject.activeInHierarchy)
                {
                    _burning.RemoveAt(i);
                    _dirty = true;
                    continue;
                }
                // Coalesce continuous exposure: flat armor absorption must not erase every 0.25-second micro-hit.
                burning.DamageCredit += TickSeconds;
                if (burning.DamageCredit >= 1f)
                {
                    burning.Health.ApplyDamage(burning.Zone, 0f, NapalmPatchSet.BurnPerSecond);
                    burning.DamageCredit -= 1f;
                }
                burning.Remaining -= TickSeconds;
                if (burning.Remaining <= 0f) { _burning.RemoveAt(i); _dirty = true; }
            }
            // Clients count fuel down locally; only starts, refreshes, expiries and surface moves are sent.
            if (!_dirty) return;
            _dirty = false;
            NapalmPatchSnapshot[] snapshot = new NapalmPatchSnapshot[_fuel.Patches.Count + _burning.Count];
            for (int i = 0; i < _fuel.Patches.Count; i++)
            {
                NapalmPatchSet.Patch patch = _fuel.Patches[i];
                snapshot[i] = new NapalmPatchSnapshot
                {
                    Id = patch.Id, Point = patch.Point, Normal = patch.Normal, Remaining = patch.Remaining,
                };
            }
            for (int i = 0; i < _burning.Count; i++)
            {
                BurningBody burning = _burning[i];
                snapshot[_fuel.Patches.Count + i] = new NapalmPatchSnapshot
                {
                    Id = burning.Id, IsBody = true, BodyObjectId = burning.Health.ObjectId, Zone = burning.Zone,
                    Point = burning.Limb != null ? burning.Limb.bounds.center : burning.Health.transform.position + Vector3.up,
                    Normal = Vector3.up, Remaining = burning.Remaining,
                };
            }
            ObserversSyncPatches(snapshot);
        }

        [ObserversRpc(BufferLast = true, RunLocally = true)]
        private void ObserversSyncPatches(NapalmPatchSnapshot[] patches) => NapalmSurfaceVfx.Sync(patches);
    }
}
