using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Health;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    public sealed partial class CombatInteractionNetwork
    {
        private readonly HashSet<HumanHealthController> _sprayHits = new();

        [Server]
        private void ServerFireNapalm(RangedWeaponItemExtension weapon, Ray aimRay, Vector3 muzzle)
        {
            _sprayHits.Clear();
            HumanHealthController self = GetComponentInChildren<HumanHealthController>();
            SubSystems.TryGet(out NapalmSubSystem napalm);
            Vector3 centre = aimRay.direction.normalized;
            Vector3 endpoint = muzzle + centre * NapalmSprayRules.Range;
            bool landed = false;
            float nearestSurface = float.MaxValue;
            Vector3 nearestPoint = default;
            LayerMask cover = LayerMask.GetMask("Default", "Walls");
            for (int i = 0; i < NapalmSprayRules.RayCount; i++)
            {
                Vector3 direction = NapalmSprayRules.Direction(centre, i);
                // Ignore triggers: fuel landing on an invisible interaction volume burns in mid-air.
                bool physical = LineOfSight.TryGetFirstHit(muzzle, direction, NapalmSprayRules.Range, cover, out RaycastHit hit,
                    QueryTriggerInteraction.Ignore);
                float stop = physical ? hit.distance : NapalmSprayRules.Range;
                bool living = ZoneTargetResolver.TryResolveHoverZone(new Ray(muzzle, direction), self, stop,
                    out BodyZone zone, out HumanHealthController health, out Vector3 point, out Collider limb);
                if (living && LineOfSight.HasLineOfSight(muzzle, point, cover, out _, QueryTriggerInteraction.Ignore))
                {
                    if (_sprayHits.Add(health))
                    {
                        ServerApplyLivingHit(weapon.Profile, health, zone);
                        if (napalm != null) napalm.Ignite(health, zone, limb);
                    }
                    landed = true;
                }
                else if (physical && napalm != null)
                {
                    napalm.Deposit(hit);
                    if (hit.distance < nearestSurface) { nearestSurface = hit.distance; nearestPoint = hit.point; }
                }
            }
            // The trade-off of a short-range fuel weapon: point-blank spray splashes back onto the shooter.
            if (napalm != null && self != null && nearestSurface < NapalmSprayRules.BlowbackRange)
                napalm.IgniteNearest(self, nearestPoint);
            ClearMeleeAimPoint();
            // This endpoint defines aim only. Persistent flames come from actual server surface contacts,
            // never from the cursor point, and the spray itself always travels the full short-range fan.
            ServerNotifyRangedFireState(weapon, landed, false, endpoint, centre, endpoint, Vector3.zero);
        }
    }
}
