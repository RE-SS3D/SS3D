using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Health;
using System.Collections.Generic;

namespace EditorTests
{
    public class ShockSimulationTests
    {
        private const float M4Brute = 18f;
        private const int M4Magazine = 30;

        /// <summary>The operative hardsuit (SyndicateHardsuit.prefab): one integrity pool over every zone but the head.</summary>
        private static ArmorProfile Hardsuit => new()
        {
            BruteAbsorption = 8f,
            BurnAbsorption = 5f,
            MaxIntegrity = 160f,
            CoveredZones = BodyZoneMask.Chest | BodyZoneMask.Groin | BodyZoneMask.LeftArm | BodyZoneMask.RightArm
                | BodyZoneMask.LeftLeg | BodyZoneMask.RightLeg,
        };

        /// <summary>The server's damage path in <see cref="HumanHealthController.ApplyDamage(BodyZone, float, float)"/>, without Unity.</summary>
        private sealed class Body
        {
            public readonly ZoneDamageState[] Zones = new ZoneDamageState[HealthConstants.ZoneCount];
            public readonly List<OrganState> Organs = new()
            {
                OrganState.Default(OrganType.Brain),
                OrganState.Default(OrganType.Heart),
                OrganState.Default(OrganType.LeftLung),
                OrganState.Default(OrganType.RightLung),
                OrganState.Default(OrganType.Liver),
            };
            public SystemicPools Pools = SystemicPools.Default;
            public ShockState Shock = ShockState.None;
            public HealthState State = HealthState.Healthy;
            public ArmorProfile? Armor;
            public float ArmorIntegrity;

            public Body(ArmorProfile? armor = null)
            {
                for (int i = 0; i < Zones.Length; i++)
                {
                    Zones[i] = ZoneDamageState.Default;
                }

                Armor = armor;
                ArmorIntegrity = armor?.MaxIntegrity ?? 0f;
            }

            public void Hit(BodyZone zone, float brute, float burn = 0f)
            {
                float throughBrute = brute;
                float throughBurn = burn;
                if (Armor is ArmorProfile armor && armor.CoveredZones.Contains(zone))
                {
                    (float RemainingBrute, float RemainingBurn, float IntegrityLoss) result =
                        ArmorSimulation.ResolveAbsorption(armor, ArmorIntegrity, brute, burn);
                    throughBrute = result.RemainingBrute;
                    throughBurn = result.RemainingBurn;
                    ArmorIntegrity -= result.IntegrityLoss;
                }

                ZoneDamageState state = Zones[(int)zone];
                WoundSeverity before = HealthSimulation.ApplyZoneHit(ref state, throughBrute, throughBurn);
                Zones[(int)zone] = state;
                OrganSimulation.ApplyZoneDamageToOrgans(zone, throughBrute, throughBurn, Organs);
                Shock = ShockSimulation.AddPain(Shock, ShockSimulation.HitPain(
                    zone, throughBrute, throughBurn, brute - throughBrute, burn - throughBurn, before, state.Severity));
                Publish();
            }

            /// <summary>The 1 Hz tick in <see cref="HumanHealthController.TickHealth"/>.</summary>
            public void Tick(int ticks = 1)
            {
                for (int i = 0; i < ticks; i++)
                {
                    OrganSimulation.TickOrganFunction(Pools, Organs);
                    Pools = HealthSimulation.TickPools(Pools, Zones, Organs);
                    Shock = ShockSimulation.Tick(Shock, ShockSimulation.Floor(Zones, Pools.BloodVolumeRatio));
                    Publish();
                }
            }

            /// <summary>Lets every pending hit turn into shock, with no decay or bleeding in between.</summary>
            public void Settle()
            {
                while (Shock.IncomingPain > 0f)
                {
                    Shock = ShockSimulation.Tick(Shock, 0f);
                }

                Publish();
            }

            public void Bandage(BodyZone zone)
            {
                ZoneDamageState state = Zones[(int)zone];
                state.BleedingRate = 0f;
                Zones[(int)zone] = state;
            }

            /// <summary>Sets shock with nothing pending and re-evaluates the state, as a published snapshot would.</summary>
            public void SetShock(float shock)
            {
                Shock = new ShockState { Shock = shock };
                Publish();
            }

            public HealthSnapshot Snapshot => HealthSimulation.BuildSnapshot(Pools, Zones, Organs, Shock, State);

            private void Publish() => State = Snapshot.State;
        }

        /// <summary>Hits needed, landing one after another at <paramref name="pattern"/>, before the settled body reaches <paramref name="state"/>.</summary>
        private static int HitsUntil(HealthState state, BodyZone[] pattern, ArmorProfile? armor = null)
        {
            for (int hit = 1; hit <= M4Magazine * 2; hit++)
            {
                var probe = new Body(armor);
                for (int i = 1; i <= hit; i++)
                {
                    probe.Hit(pattern[(i - 1) % pattern.Length], M4Brute);
                }

                probe.Settle();
                if (probe.State == state || (state == HealthState.Downed && probe.State == HealthState.Critical))
                {
                    return hit;
                }
            }

            return int.MaxValue;
        }

        private static readonly BodyZone[] CenterMass = { BodyZone.Chest, BodyZone.Groin };
        private static readonly BodyZone[] Arms = { BodyZone.LeftArm, BodyZone.RightArm };

        [Test]
        public void ZoneWeightsFavourCenterMassAndHead()
        {
            Assert.AreEqual(HealthConstants.ShockTorsoZoneWeight, ShockSimulation.ZoneWeight(BodyZone.Chest));
            Assert.AreEqual(HealthConstants.ShockTorsoZoneWeight, ShockSimulation.ZoneWeight(BodyZone.Groin));
            Assert.AreEqual(HealthConstants.ShockHeadZoneWeight, ShockSimulation.ZoneWeight(BodyZone.Head));
            Assert.AreEqual(HealthConstants.ShockLimbZoneWeight, ShockSimulation.ZoneWeight(BodyZone.LeftLeg));
            Assert.Greater(
                ShockSimulation.HitPain(BodyZone.Chest, 5f, 0f, 0f, 0f, WoundSeverity.None, WoundSeverity.None),
                ShockSimulation.HitPain(BodyZone.RightArm, 5f, 0f, 0f, 0f, WoundSeverity.None, WoundSeverity.None));
        }

        [Test]
        public void BurnHurtsMoreThanTheSameBrute()
        {
            float brute = ShockSimulation.HitPain(BodyZone.Chest, 5f, 0f, 0f, 0f, WoundSeverity.None, WoundSeverity.None);
            float burn = ShockSimulation.HitPain(BodyZone.Chest, 0f, 5f, 0f, 0f, WoundSeverity.None, WoundSeverity.None);
            Assert.AreEqual(brute * HealthConstants.ShockBurnTypeWeight, burn, 1e-4f);
        }

        [Test]
        public void TierSpikesFireOncePerTierCrossed()
        {
            float all = HealthConstants.ShockSpikeBruised + HealthConstants.ShockSpikeWound + HealthConstants.ShockSpikeSevere;
            Assert.AreEqual(all, ShockSimulation.TierSpikes(BodyZone.Chest, WoundSeverity.None, WoundSeverity.Severe), 1e-4f);
            Assert.AreEqual(0f, ShockSimulation.TierSpikes(BodyZone.Chest, WoundSeverity.Severe, WoundSeverity.Severe));
            Assert.AreEqual(
                HealthConstants.ShockSpikeSevered * HealthConstants.ShockLimbSpikeScale,
                ShockSimulation.TierSpikes(BodyZone.LeftArm, WoundSeverity.Disabled, WoundSeverity.Severed), 1e-4f);

            var body = new Body();
            body.Hit(BodyZone.Chest, 12f);
            Assert.AreEqual(12f * HealthConstants.ShockTorsoZoneWeight + HealthConstants.ShockSpikeBruised, body.Shock.IncomingPain, 1e-4f);
            body.Hit(BodyZone.Chest, 1f);
            Assert.AreEqual(13f * HealthConstants.ShockTorsoZoneWeight + HealthConstants.ShockSpikeBruised, body.Shock.IncomingPain, 1e-4f);
        }

        [Test]
        public void ArmorAbsorbedDamageStillBleedsThroughAsShock()
        {
            float armored = ShockSimulation.HitPain(BodyZone.Chest, 0f, 0f, 18f, 0f, WoundSeverity.None, WoundSeverity.None);
            Assert.AreEqual(18f * HealthConstants.ArmorShockFraction * HealthConstants.ShockTorsoZoneWeight, armored, 1e-4f);
            Assert.Greater(armored, 0f);
            Assert.Less(armored, ShockSimulation.HitPain(BodyZone.Chest, 18f, 0f, 0f, 0f, WoundSeverity.None, WoundSeverity.None));
        }

        [Test]
        public void IncomingPainBecomesShockOverTheBuildWindow()
        {
            var body = new Body();
            body.Hit(BodyZone.Chest, 18f);
            float pain = body.Shock.IncomingPain;
            Assert.AreEqual(0f, body.Shock.Shock, "a hit does not become shock instantly");
            Assert.AreEqual(pain, body.Snapshot.IncomingPain, "the snapshot carries the pending pain for the warning cue");

            int ticks = 0;
            while (body.Shock.IncomingPain > 0f)
            {
                body.Shock = ShockSimulation.Tick(body.Shock, 0f);
                ticks++;
                Assert.LessOrEqual(ticks, 10);
            }

            Assert.AreEqual((int)HealthConstants.ShockBuildSeconds, ticks);
            Assert.AreEqual(pain, body.Shock.Shock, 1e-3f);
        }

        [Test]
        public void ANewHitRespreadsAllPendingPain()
        {
            ShockState state = ShockSimulation.AddPain(ShockState.None, 30f);
            state = ShockSimulation.Tick(state, 0f);
            state = ShockSimulation.AddPain(state, 10f);
            for (int i = 0; i < (int)HealthConstants.ShockBuildSeconds; i++)
            {
                state = ShockSimulation.Tick(state, 0f);
            }

            Assert.AreEqual(0f, state.IncomingPain);
            Assert.AreEqual(40f, state.Shock, 1e-3f);
        }

        [Test]
        public void ShockDecaysDownToTheOpenWoundFloor()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            zones[(int)BodyZone.Chest] = new ZoneDamageState { Severity = WoundSeverity.Severe, BleedingRate = 1f };
            float floor = ShockSimulation.Floor(zones, 1f);
            Assert.AreEqual(HealthConstants.ShockFloorSevere, floor);

            var state = new ShockState { Shock = 20f };
            state = ShockSimulation.Tick(state, floor);
            Assert.AreEqual(20f - HealthConstants.ShockDecayPerSecond, state.Shock, 1e-4f);
            for (int i = 0; i < 20; i++)
            {
                state = ShockSimulation.Tick(state, floor);
            }

            Assert.AreEqual(floor, state.Shock, 1e-4f);
        }

        [Test]
        public void ShockDoesNotDecayWhilePainIsPending()
        {
            ShockState state = ShockSimulation.AddPain(new ShockState { Shock = 50f }, 9f);
            state = ShockSimulation.Tick(state, 0f);
            Assert.AreEqual(53f, state.Shock, 1e-4f);
        }

        [Test]
        public void ABandageDropsTheFloor()
        {
            var body = new Body();
            body.Hit(BodyZone.Chest, 55f);
            float open = ShockSimulation.Floor(body.Zones, 1f);
            Assert.AreEqual(HealthConstants.ShockFloorSevere, open);

            body.Bandage(BodyZone.Chest);
            Assert.AreEqual(0f, ShockSimulation.Floor(body.Zones, 1f));
        }

        [Test]
        public void LowBloodRaisesTheFloor()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            Assert.AreEqual(0f, ShockSimulation.Floor(zones, 1f));
            Assert.AreEqual(
                (1f - HealthConstants.CriticalBloodVolumeRatio) * HealthConstants.LowBloodShockScale,
                ShockSimulation.Floor(zones, HealthConstants.CriticalBloodVolumeRatio), 1e-4f);

            var state = new ShockState { Shock = 0f };
            state = ShockSimulation.Tick(state, 30f);
            Assert.AreEqual(HealthConstants.ShockDecayPerSecond, state.Shock, 1e-4f, "shock climbs towards a raised floor at the decay rate");
        }

        [Test]
        public void DownedHasHysteresis()
        {
            Assert.IsFalse(ShockSimulation.IsDowned(HealthConstants.ShockDownedThreshold - 1f, true, wasDowned: false));
            Assert.IsTrue(ShockSimulation.IsDowned(HealthConstants.ShockDownedThreshold, true, wasDowned: false));
            Assert.IsTrue(ShockSimulation.IsDowned(HealthConstants.ShockStandThreshold, true, wasDowned: true));
            Assert.IsFalse(ShockSimulation.IsDowned(HealthConstants.ShockStandThreshold - 0.1f, true, wasDowned: true));

            float between = (HealthConstants.ShockDownedThreshold + HealthConstants.ShockStandThreshold) * 0.5f;
            var body = new Body();
            body.SetShock(HealthConstants.ShockDownedThreshold);
            Assert.AreEqual(HealthState.Downed, body.State);

            body.SetShock(between);
            Assert.AreEqual(HealthState.Downed, body.State, "between the lines a downed body stays down");

            body.SetShock(HealthConstants.ShockStandThreshold - 1f);
            Assert.AreEqual(HealthState.Healthy, body.State);

            body.SetShock(between);
            Assert.AreEqual(HealthState.Healthy, body.State, "between the lines a standing body stays up");
        }

        [Test]
        public void DownedAtSixtyWithWoundsBandagedStandsInAboutSevenSeconds()
        {
            var body = new Body();
            body.SetShock(HealthConstants.ShockDownedThreshold);
            Assert.AreEqual(HealthState.Downed, body.State);

            int seconds = 0;
            while (body.State == HealthState.Downed)
            {
                body.Tick();
                seconds++;
                Assert.LessOrEqual(seconds, 30);
            }

            Assert.That(seconds, Is.InRange(6, 8));
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void BothLegsOutForceDownedWhateverTheShock(bool splintLeft, bool severRightOnly, bool expectDowned)
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            var disabled = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true, IsSplinted = splintLeft };
            var severed = new ZoneDamageState { Severity = WoundSeverity.Severed, IsDisabled = true, IsSevered = true };
            zones[(int)BodyZone.LeftLeg] = severRightOnly ? ZoneDamageState.Default : disabled;
            zones[(int)BodyZone.RightLeg] = severed;

            var organs = new List<OrganState> { OrganState.Default(OrganType.Brain), OrganState.Default(OrganType.Heart) };
            HealthState state = HealthSimulation.EvaluateHealthState(SystemicPools.Default, organs, zones, 0f, HealthState.Healthy);
            Assert.AreEqual(expectDowned ? HealthState.Downed : HealthState.Healthy, state);
        }

        [Test]
        public void ShockAtTheCriticalLineSetsHighShock()
        {
            var body = new Body();
            body.Shock = new ShockState { Shock = HealthConstants.ShockCriticalThreshold };
            HealthSnapshot snapshot = body.Snapshot;
            Assert.AreEqual(HealthState.Critical, snapshot.State);
            Assert.IsTrue((snapshot.CriticalFlags & HealthCriticalFlags.HighShock) != 0);
            Assert.IsFalse(snapshot.CanUseArms, "Critical from shock incapacitates like any other Critical");

            body.Shock = new ShockState { Shock = HealthConstants.ShockCriticalThreshold - 1f };
            Assert.IsTrue((body.Snapshot.CriticalFlags & HealthCriticalFlags.HighShock) == 0);
        }

        [Test]
        public void ADownedBodyStaysConsciousAndCanAct()
        {
            var body = new Body();
            body.Shock = new ShockState { Shock = HealthConstants.ShockDownedThreshold };
            HealthSnapshot snapshot = body.Snapshot;
            Assert.AreEqual(HealthState.Downed, snapshot.State);
            Assert.IsTrue(snapshot.IsConscious);
            Assert.IsTrue(snapshot.CanUseArms);
        }

        [Test]
        public void ShockAloneNeverDrainsTheBrainOrHeart()
        {
            var body = new Body();
            for (int i = 0; i < 120; i++)
            {
                body.Shock = new ShockState { Shock = 500f };
                body.Tick();
            }

            Assert.AreEqual(HealthState.Critical, body.State);
            Assert.AreEqual(100f, OrganSimulation.GetStoredOrganFunction(body.Organs, OrganType.Brain));
            Assert.AreEqual(100f, OrganSimulation.GetStoredOrganFunction(body.Organs, OrganType.Heart));
        }

        [Test]
        public void ShockLowersTheStaminaCap()
        {
            Assert.AreEqual(1f, ShockSimulation.StaminaCapScale(0f));
            Assert.AreEqual(HealthConstants.ShockStaminaCapFloor, ShockSimulation.StaminaCapScale(HealthConstants.ShockDownedThreshold), 1e-4f);
            Assert.AreEqual(HealthConstants.ShockStaminaCapFloor, ShockSimulation.StaminaCapScale(500f), 1e-4f);
            Assert.AreEqual(
                (1f + HealthConstants.ShockStaminaCapFloor) * 0.5f,
                ShockSimulation.StaminaCapScale(HealthConstants.ShockDownedThreshold * 0.5f), 1e-4f);
        }

        [Test]
        public void M4CenterMassBurstDownsInSixToNineHitsAndCriticalsInTenToFourteen()
        {
            Assert.That(HitsUntil(HealthState.Downed, CenterMass), Is.InRange(6, 9));
            Assert.That(HitsUntil(HealthState.Critical, CenterMass), Is.InRange(10, 14));
        }

        [Test]
        public void M4OnTheArmsLimpsButDoesNotDownBeforeAboutTwelveHits()
        {
            Assert.That(HitsUntil(HealthState.Downed, Arms), Is.GreaterThanOrEqualTo(12));
        }

        [Test]
        public void M4OnBothLegsDownsOnceBothLegsAreDisabled()
        {
            int hits = HitsUntil(HealthState.Downed, new[] { BodyZone.LeftLeg, BodyZone.RightLeg });
            int legHitsToDisable = (int)System.Math.Ceiling(HealthConstants.DisabledThreshold / M4Brute);
            Assert.LessOrEqual(hits, legHitsToDisable * 2);
        }

        [Test]
        public void HardsuitOperativeTakesAboutTwiceTheHitsToDown()
        {
            int unarmored = HitsUntil(HealthState.Downed, CenterMass);
            int armored = HitsUntil(HealthState.Downed, CenterMass, Hardsuit);
            Assert.That(armored, Is.InRange((int)System.Math.Ceiling(unarmored * 1.5f), unarmored * 3));
            Assert.LessOrEqual(armored, M4Magazine, "a focused response still drops a suited operative within a magazine");
        }
    }
}
