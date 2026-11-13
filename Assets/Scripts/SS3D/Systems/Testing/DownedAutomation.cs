using FishNet;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using System;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Automation steps for mppm:downed (shock-and-downed plan PR 6): the server shoots a remote player Downed and heals
    /// it; the remote client checks its own body crawls, keeps its controller and hands, and stands again.
    /// </summary>
    public static class DownedAutomation
    {
        private static readonly BodyZone[] CenterMass = { BodyZone.Chest, BodyZone.Groin };

        /// <summary>Server: M4 rounds into the first remote body, two at a time, until it is Downed.</summary>
        public static IEnumerator DownRemoteBody(float timeoutSeconds)
        {
            HumanHealthController health = RemoteHealth("down_client");
            float start = Time.time;
            int rounds = 0;
            while (health.Snapshot.State != HealthState.Downed)
            {
                if (health.Snapshot.State != HealthState.Healthy || Time.time - start > timeoutSeconds)
                {
                    throw new InvalidOperationException(
                        $"down_client: {health.Snapshot.State} with shock {health.Snapshot.Shock:F0} after {rounds} rounds");
                }

                if (health.Snapshot.IncomingPain <= 0f)
                {
                    for (int i = 0; i < 2; i++, rounds++)
                    {
                        CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.M4, health, CenterMass[rounds % 2]);
                    }
                }

                yield return null;
            }
        }

        /// <summary>Server: heals every remote body fully (zones, pools, organs, shock).</summary>
        public static void HealRemoteBodies()
        {
            foreach (Entity body in SubSystems.Get<EntitySubSystem>().SpawnedPlayers)
            {
                if (body == null || body.IsOwner || !body.TryGetComponent(out HumanHealthController health))
                    continue;

                for (int i = 0; i < HealthConstants.ZoneCount; i++)
                {
                    health.ApplyTreatment((BodyZone)i, 1000f, 1000f, stopBleeding: true);
                }

                health.RestoreSystemicPools();
                health.RestoreOrgans();
            }
        }

        /// <summary>Client: the owned body is Downed as this process sees it — crawling under the animator, not ragdolled.</summary>
        public static IEnumerator AssertOwnBodyDowned(float timeoutSeconds)
        {
            Entity entity = OwnBody();
            Ragdoll ragdoll = entity.GetComponent<Ragdoll>();
            HumanoidBodyStateMachine body = entity.GetComponent<HumanoidBodyStateMachine>();
            HumanHealthController health = entity.GetComponent<HumanHealthController>();
            yield return Wait(() => ragdoll.IsDowned && !ragdoll.IsFallingDowned && body.Snapshot.State == BodyState.Crawling && body.Snapshot.IsCrawling,
                timeoutSeconds, "assert_downed crawl");

            if (ragdoll.IsKnockedDown) throw new InvalidOperationException("assert_downed: a Downed body is ragdolled");
            if (health.Snapshot.State != HealthState.Downed) throw new InvalidOperationException($"assert_downed: health reads {health.Snapshot.State}");
            if (!entity.GetComponent<CharacterController>().enabled) throw new InvalidOperationException("assert_downed: the owner can't crawl (controller off)");
            if (!body.Capabilities.CanMove || !body.Capabilities.CanUseHands) throw new InvalidOperationException("assert_downed: crawling lost movement or hands");
            if (!entity.GetComponent<SS3D.Systems.Inventory.Containers.Hands>().SelectedHand.CanUseHand)
                throw new InvalidOperationException("assert_downed: the hand is unusable while Downed");
        }

        /// <summary>Client: the owned body is back on its feet.</summary>
        public static IEnumerator AssertOwnBodyStood(float timeoutSeconds)
        {
            Entity entity = OwnBody();
            Ragdoll ragdoll = entity.GetComponent<Ragdoll>();
            HumanoidBodyStateMachine body = entity.GetComponent<HumanoidBodyStateMachine>();
            yield return Wait(() => ragdoll.Presentation == BodyPresentationState.Locomotion
                && body.Snapshot.State == BodyState.Locomotion && !body.Snapshot.IsCrawling, timeoutSeconds, "assert_stood");
        }

        private static Entity OwnBody()
        {
            if (!SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity))
                throw new InvalidOperationException("No owned body");
            return entity;
        }

        private static HumanHealthController RemoteHealth(string step)
        {
            foreach (Entity body in SubSystems.Get<EntitySubSystem>().SpawnedPlayers)
            {
                if (body != null && !body.IsOwner && body.TryGetComponent(out HumanHealthController health))
                    return health;
            }

            throw new InvalidOperationException($"{step}: no remote body");
        }

        private static IEnumerator Wait(Func<bool> condition, float timeoutSeconds, string label)
        {
            float start = Time.time;
            while (!condition())
            {
                if (Time.time - start > timeoutSeconds)
                    throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for {label}");
                yield return null;
            }
        }
    }
}
