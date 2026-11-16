using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using System.Collections.Generic;

namespace EditorTests
{
    /// <summary>What a Downed body can still do, and the limb rules (health.md §4, §5).</summary>
    public class DownedBodyTests
    {
        private static ZoneDamageState[] HealthyZones()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            return zones;
        }

        private static List<OrganState> Organs() => new()
        {
            OrganState.Default(OrganType.Brain),
            OrganState.Default(OrganType.Heart),
        };

        private static HealthSnapshot Downed() => HealthSimulation.BuildSnapshot(
            SystemicPools.Default, HealthyZones(), Organs(),
            new ShockState { Shock = HealthConstants.ShockDownedThreshold }, HealthState.Healthy);

        [Test]
        public void DownedMapsToTheDownedPresentationNotACollapse()
        {
            HealthSnapshot snapshot = Downed();
            Assert.AreEqual(HealthState.Downed, snapshot.State);
            Assert.AreEqual(BodyPresentationState.Downed, BodyPresentationIntent.FromSnapshot(snapshot));
            Assert.IsFalse(BodyPresentationState.Downed.IsRagdolled());
            Assert.IsTrue(BodyPresentationState.Collapsed.IsRagdolled());
        }

        [Test]
        public void CriticalStillCollapsesEvenWithBrokenLegs()
        {
            ZoneDamageState[] zones = HealthyZones();
            zones[(int)BodyZone.LeftLeg] = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true };
            zones[(int)BodyZone.RightLeg] = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true };
            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(
                SystemicPools.Default, zones, Organs(),
                new ShockState { Shock = HealthConstants.ShockCriticalThreshold }, HealthState.Downed);
            Assert.AreEqual(BodyPresentationState.Collapsed, BodyPresentationIntent.FromSnapshot(snapshot));
        }

        [Test]
        public void DownedCrawlsSlowlyAndCannotSprintButKeepsItsHands()
        {
            HealthSnapshot snapshot = Downed();
            Assert.AreEqual(HealthConstants.DownedCrawlSpeedMultiplier, snapshot.MovementSpeedMultiplier);
            Assert.IsFalse(snapshot.CanSprint);
            Assert.IsTrue(snapshot.CanUseArms);
            Assert.IsTrue(HealthSnapshot.Default.CanSprint);
        }

        [Test]
        public void TwoHandedWeaponsAreRefusedWhileDownedAndOneHandedOnesSpreadWide()
        {
            HealthSnapshot downed = Downed();
            Assert.IsFalse(DownedFireRules.MayFire(downed, requiresBothHands: true));
            Assert.IsTrue(DownedFireRules.MayFire(downed, requiresBothHands: false));
            Assert.AreEqual(HealthConstants.DownedSpreadMultiplier, DownedFireRules.SpreadMultiplier(downed));

            Assert.IsTrue(DownedFireRules.MayFire(HealthSnapshot.Default, requiresBothHands: true));
            Assert.AreEqual(1f, DownedFireRules.SpreadMultiplier(HealthSnapshot.Default));
        }

        [Test]
        public void ADisabledArmLosesItsHandUntilSplinted()
        {
            ZoneDamageState[] zones = HealthyZones();
            zones[(int)BodyZone.LeftArm] = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true };
            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, Organs());
            Assert.IsFalse(snapshot.CanUseArm(BodyZone.LeftArm));
            Assert.IsTrue(snapshot.CanUseArm(BodyZone.RightArm));

            zones[(int)BodyZone.LeftArm].IsSplinted = true;
            snapshot = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, Organs());
            Assert.IsTrue(snapshot.CanUseArm(BodyZone.LeftArm));
        }

        [Test]
        public void HelpUpNeedsShockUnderTheDownedLineAndALegToStandOn()
        {
            HealthSnapshot downedHard = Downed();
            Assert.IsFalse(ShockSimulation.CanBeHelpedUp(downedHard), "at the Downed line it is too soon");

            HealthSnapshot recovering = HealthSimulation.BuildSnapshot(
                SystemicPools.Default, HealthyZones(), Organs(),
                new ShockState { Shock = HealthConstants.ShockDownedThreshold - 5f }, HealthState.Downed);
            Assert.AreEqual(HealthState.Downed, recovering.State);
            Assert.IsTrue(ShockSimulation.CanBeHelpedUp(recovering));

            ZoneDamageState[] legs = HealthyZones();
            legs[(int)BodyZone.LeftLeg] = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true };
            legs[(int)BodyZone.RightLeg] = new ZoneDamageState { Severity = WoundSeverity.Disabled, IsDisabled = true };
            HealthSnapshot brokenLegs = HealthSimulation.BuildSnapshot(SystemicPools.Default, legs, Organs());
            Assert.AreEqual(HealthState.Downed, brokenLegs.State);
            Assert.IsFalse(ShockSimulation.CanBeHelpedUp(brokenLegs), "two broken legs need a splint first");

            legs[(int)BodyZone.LeftLeg].IsSplinted = true;
            HealthSnapshot splinted = HealthSimulation.BuildSnapshot(SystemicPools.Default, legs, Organs(),
                new ShockState { Shock = HealthConstants.ShockStandThreshold + 5f }, HealthState.Downed);
            Assert.AreEqual(HealthState.Downed, splinted.State);
            Assert.IsTrue(ShockSimulation.CanBeHelpedUp(splinted));

            Assert.IsFalse(ShockSimulation.CanBeHelpedUp(HealthSnapshot.Default), "a standing body has nothing to help");
        }

        [Test]
        public void HelpingUpStandsTheBodyAndLeavesItUnsteady()
        {
            ShockState shock = ShockSimulation.HelpUp(new ShockState { Shock = HealthConstants.ShockDownedThreshold - 5f, IncomingPain = 4f });
            Assert.Less(shock.Shock, HealthConstants.ShockStandThreshold);
            Assert.AreEqual(4f, shock.IncomingPain, "pain still on its way keeps coming");

            HealthSnapshot up = HealthSimulation.BuildSnapshot(SystemicPools.Default, HealthyZones(), Organs(), shock, HealthState.Downed);
            Assert.AreEqual(HealthState.Healthy, up.State);

            HealthSnapshot unsteady = HealthSimulation.ApplyUnsteady(up, true);
            Assert.IsTrue(unsteady.IsUnsteady);
            Assert.IsFalse(unsteady.CanSprint);
            Assert.AreEqual(up.MovementSpeedMultiplier * HealthConstants.UnsteadySpeedMultiplier, unsteady.MovementSpeedMultiplier, 1e-4f);
            Assert.AreEqual(up, HealthSimulation.ApplyUnsteady(up, false));
        }

        [Test]
        public void ASevereHeadWoundSpikesShockAndBlurs()
        {
            float chest = ShockSimulation.TierSpikes(BodyZone.Chest, WoundSeverity.Wound, WoundSeverity.Severe);
            float head = ShockSimulation.TierSpikes(BodyZone.Head, WoundSeverity.Wound, WoundSeverity.Severe);
            Assert.AreEqual(chest + HealthConstants.ShockHeadSevereSpike, head, 1e-4f);
            Assert.AreEqual(0f, ShockSimulation.TierSpikes(BodyZone.Head, WoundSeverity.Severe, WoundSeverity.Severe));

            ZoneDamageState[] zones = HealthyZones();
            zones[(int)BodyZone.Head] = new ZoneDamageState { Severity = WoundSeverity.Severe };
            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(SystemicPools.Default, zones, Organs());
            Assert.AreEqual(HealthConstants.HeadSevereBlur, snapshot.HeadTraumaBlur);
            Assert.IsTrue(snapshot.IsConscious, "a head wound blurs; it does not knock out by itself");
            Assert.GreaterOrEqual(HealthScreenEffectMapper.Compute(snapshot).Concussion, HealthConstants.HeadSevereBlur);
        }
    }
}
