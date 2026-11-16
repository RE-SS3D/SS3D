using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: throw mode's server path (humanoid animation pass Phase 3): the item stays in hand until the clip's release frame, lands where it was aimed or at its weight's range, arms a minibomb at release, and stays in hand when a knockdown interrupts the wind-up · Not cheaper because: the launch runs through the live hand container, rigidbody flight against the room's colliders, the networked trigger and the ragdoll together
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class ThrowScenarios : ScenarioFixture
    {
        private const string Wrench = "772da386eb7b01745913050fee544c32";
        private const string OxygenTank = "5479411218b869144b7d09d6db6dc58e";
        private const string Minibomb = "f811d1b8a792159e396e4b258540cd7e";

        /// <summary>The room's open floor: the body stands west of the epicenter other scenarios use and throws east.</summary>
        private static readonly Vector3 Stand = new(-2f, 0f, -1f);

        [UnityTest]
        public IEnumerator Wrench_LeavesAtTheReleaseFrame_AndLandsOnTheAimPoint()
        {
            Item wrench = GiveActiveHand(Wrench, out Hand hand);
            Vector3 target = Floor(Stand + new Vector3(3f, 0f, 0f));
            yield return Hold(0.3f);

            Assert.IsTrue(Combat.TryThrowAt(target), "The wrench can be thrown");
            float thrownAt = Time.time;
            Animator animator = Body.GetComponent<Animator>();
            yield return WaitFor(() => animator.GetCurrentAnimatorStateInfo(1).IsName(ThrowAnimation.Variants[ThrowAnimation.Overhand].StateName),
                "the upper body plays the overhand throw", 1f);
            Assert.AreSame(wrench, hand.ItemInHand, "The wrench stays in hand during the wind-up");

            yield return WaitFor(() => hand.ItemInHand != wrench, "the wrench leaves the hand", 2f);
            float releasedAfter = Time.time - thrownAt;
            Assert.That(releasedAfter, Is.InRange(ThrowAnimation.Variants[ThrowAnimation.Overhand].ReleaseSeconds - 0.05f,
                ThrowAnimation.Variants[ThrowAnimation.Overhand].ReleaseSeconds + 0.2f), "It leaves at the release frame");

            Vector3 firstContact = default;
            yield return WaitForFirstContact(wrench, point => firstContact = point);
            float miss = Planar(firstContact - target);
            Debug.Log($"[ThrowScenarios] wrench released after {releasedAfter:F2}s, first landed {miss:F2} m from the aim point");
            Assert.Less(miss, 0.6f, "The wrench comes down on the aim point");
            Assert.IsFalse(Combat.ThrowModeActive, "Throwing ends throw mode");
            wrench.Delete();
        }

        [UnityTest]
        public IEnumerator OxygenTank_AimedPastItsRange_FallsShortAtTheHeavyRange()
        {
            Item tank = GiveActiveHand(OxygenTank, out Hand hand);
            float range = ThrowRules.MaxRange(ThrowRules.ThrownWeight(tank));
            Assert.AreEqual(ThrowRules.HeavyRange, range, 0.01f, "A 5 kg tank flies the heavy range");
            Vector3 feet = Body.transform.position;
            yield return Hold(0.3f);

            Assert.IsTrue(Combat.TryThrowAt(Floor(Stand + new Vector3(8f, 0f, 0f))), "The tank can be thrown");
            yield return WaitFor(() => hand.ItemInHand != tank, "the tank leaves the hand", 2f);
            Vector3 firstContact = default;
            yield return WaitForFirstContact(tank, point => firstContact = point);
            float carried = Planar(firstContact - feet);
            Debug.Log($"[ThrowScenarios] tank first landed {carried:F2} m out (range {range:F1} m)");
            Assert.That(carried, Is.InRange(range - 0.7f, range + 0.5f), "It comes down at its range, not at the cursor");
            tank.Delete();
        }

        [UnityTest]
        public IEnumerator Minibomb_ArmsAsItLeavesTheHand_WithTheGrenadeLob()
        {
            Item bomb = GiveActiveHand(Minibomb, out Hand hand);
            TimedCharge charge = bomb.GetComponent<TimedCharge>();
            yield return Hold(0.3f);

            Assert.IsTrue(Combat.TryThrowAt(Floor(Stand + new Vector3(3f, 0f, 0f))), "The minibomb can be thrown");
            Animator animator = Body.GetComponent<Animator>();
            yield return WaitFor(() => animator.GetCurrentAnimatorStateInfo(1).IsName(ThrowAnimation.Variants[ThrowAnimation.Grenade].StateName),
                "the upper body plays the grenade lob", 1f);
            Assert.IsFalse(charge.Armed, "Not armed during the wind-up");

            yield return WaitFor(() => hand.ItemInHand != bomb, "the minibomb leaves the hand", 2f);
            Assert.IsTrue(charge.Armed, "Armed at release");
            bomb.Delete();
        }

        [UnityTest]
        public IEnumerator KnockdownBeforeRelease_KeepsTheMinibombInHand_Unarmed()
        {
            Item bomb = GiveActiveHand(Minibomb, out Hand hand);
            TimedCharge charge = bomb.GetComponent<TimedCharge>();
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            yield return Hold(0.3f);

            Assert.IsTrue(Combat.TryThrowAt(Floor(Stand + new Vector3(3f, 0f, 0f))), "The minibomb can be thrown");
            yield return Hold(0.1f);
            ragdoll.ServerKnockdown(1f);
            yield return Hold(ThrowAnimation.Variants[ThrowAnimation.Grenade].ReleaseSeconds + 0.3f);

            Assert.AreSame(bomb, hand.ItemInHand, "An interrupted throw keeps the item");
            Assert.IsFalse(charge.Armed, "and the minibomb stays unarmed");
            Assert.IsFalse(Combat.HasPendingThrow, "The cancelled throw is cleared");
            yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Locomotion, "the body gets up", 8f);
            bomb.Delete();
        }

        [UnityTest]
        public IEnumerator ThrowMode_DrawsTheLandingRing_UntilEscapeOrAThrow()
        {
            Item wrench = GiveActiveHand(Wrench, out _);
            Combat.SetThrowMode(true);
            yield return Hold(0.3f);

            var ring = (System.Collections.Generic.List<Vector2>)typeof(CombatInteractionNetwork)
                .GetField("_throwRingScreen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(Combat);
            Assert.IsTrue(Combat.ThrowModeActive, "R's toggle holds throw mode on");
            Assert.That(ring.Count, Is.GreaterThan(2), "The overlay draws a landing ring under the cursor");

            Assert.IsTrue(Combat.TryThrowAt(Floor(Stand + new Vector3(2f, 0f, 0f))), "The wrench can be thrown");
            Assert.IsFalse(Combat.ThrowModeActive, "Throwing ends throw mode");
            yield return Hold(1f);
            wrench.Delete();
        }

        private CombatInteractionNetwork Combat => Body.GetComponent<CombatInteractionNetwork>();

        private Item GiveActiveHand(string asset, out Hand hand)
        {
            EmptyHands(Body);
            Teleport(Body, new Vector3(Stand.x, Body.transform.position.y, Stand.z));
            Hands hands = Body.GetComponent<Hands>();
            hand = hands.SelectedHand;
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(asset, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(item), "The item goes in the active hand");
            return item;
        }

        private static Vector3 Floor(Vector3 point)
        {
            Assert.IsTrue(Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore), $"The room has floor at {point}");
            return hit.point;
        }

        /// <summary>Waits until the flying item first stops falling (its first touch-down), and reports where.</summary>
        private static IEnumerator WaitForFirstContact(Item item, System.Action<Vector3> landed)
        {
            Rigidbody body = item.GetComponent<Rigidbody>();
            float start = Time.time;
            while (body.linearVelocity.y > -0.5f)
            {
                Assert.Less(Time.time - start, 2f, "The item starts to fall within 2 s");
                yield return new WaitForFixedUpdate();
            }

            while (body.linearVelocity.y < -0.5f)
            {
                Assert.Less(Time.time - start, 3f, "The item lands within 3 s");
                yield return new WaitForFixedUpdate();
            }

            // Falling has just ended: the item bounced or came to rest.
            landed(item.transform.position);
        }

        private static float Planar(Vector3 delta) => new Vector2(delta.x, delta.z).magnitude;
    }
}
