using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: interaction animations (humanoid animation pass Phase 4): a pickup lands in the hand at the floor bend's contact frame and faces the item, a knockdown before contact keeps the item on the floor, a busy upper body skips the clip, and a defuse kneels for its delay and stands on completion or when the body moves · Not cheaper because: the clip choice runs on the server's interaction dispatch, the RPC and the replicated kneel bit reach the live animator, and the defuse is a real delayed interaction
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class InteractionAnimationScenarios : ScenarioFixture
    {
        private const string Wrench = "772da386eb7b01745913050fee544c32";
        private const string C4 = "d5222e435d7aa2d45b959a73ebc874c9";

        /// <summary>The room's open floor, as in ThrowScenarios.</summary>
        private static readonly Vector3 Stand = new(-2f, 0f, -1f);

        [UnityTest]
        public IEnumerator FloorPickup_LandsInHandWhenTheHandReachesIt()
        {
            PlaceBody();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Vector3 itemPosition = Body.transform.position + new Vector3(0f, 0.05f, 0.6f);
            Item wrench = SubSystems.Get<ItemSubSystem>().SpawnItem(Wrench, itemPosition, Quaternion.identity);
            Body.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return Hold(0.5f);

            Animator animator = Body.GetComponent<Animator>();
            string refusal = Interactions.RunInteractionForTesting(wrench.gameObject, wrench.transform.position, "Pickup", IntentType.Help);
            Assert.IsNull(refusal, refusal);
            float clickedAt = Time.time;
            InteractionAnimation.Clip bend = InteractionAnimation.All[InteractionAnimation.FloorPickup];
            yield return WaitFor(() => InState(animator, 0, bend.StateName), "the body bends for the floor pickup", 0.3f);
            Assert.AreNotSame(wrench, hand.ItemInHand, "The wrench stays on the floor until the hand gets there");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(Body.transform.eulerAngles.y, 0f)), 15f, "The body turned to face the item");

            yield return WaitFor(() => hand.ItemInHand == wrench, "the wrench lands in the hand", bend.ContactSeconds + 0.5f);
            float inHandAfter = Time.time - clickedAt;
            Debug.Log($"[InteractionAnimationScenarios] wrench in hand {inHandAfter:F2}s after the click (contact {bend.ContactSeconds:F2}s)");
            Assert.That(inHandAfter, Is.InRange(bend.ContactSeconds - 0.05f, bend.ContactSeconds + 0.25f),
                "It lands at the contact frame (revised Decision 15)");

            yield return WaitFor(() => !InState(animator, 0, bend.StateName), "the bend ends", bend.PlaySeconds + 1f);
            wrench.Delete();
        }

        // Playtest bug "Item miniatures": the height morph scales the armature the hands sit under, and
        // each pickup and drop multiplied the item by that scale, so a short holder shrank it a bit more
        // every time.
        [UnityTest]
        public IEnumerator PickupAndDrop_KeepTheItemsSize_ForAShortHolder()
        {
            PlaceBody();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Transform armature = Body.GetComponent<HumanoidRigReferences>().ArmatureRoot;
            Vector3 bodyScale = armature.localScale;
            Item wrench = SubSystems.Get<ItemSubSystem>().SpawnItem(Wrench, Body.transform.position, Quaternion.identity);
            Vector3 ownScale = wrench.transform.lossyScale;

            try
            {
                armature.localScale = Vector3.one * 0.88f; // the shortest the height slider makes a body
                for (int i = 0; i < 5; i++)
                {
                    Assert.IsTrue(hand.Container.AddItem(wrench), "The wrench goes in the active hand");
                    yield return null;
                    hand.Container.RemoveItem(wrench);
                    yield return null;
                }

                Assert.That(Vector3.Distance(wrench.transform.lossyScale, ownScale), Is.LessThan(1e-3f),
                    $"The dropped wrench keeps its own size ({ownScale}), not {wrench.transform.lossyScale}");
            }
            finally
            {
                armature.localScale = bodyScale;
                wrench.Delete();
            }
        }

        [UnityTest]
        public IEnumerator KnockdownBeforeContact_LeavesTheItemOnTheFloor()
        {
            PlaceBody();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Item wrench = SubSystems.Get<ItemSubSystem>().SpawnItem(
                Wrench, Body.transform.position + new Vector3(0f, 0.05f, 0.6f), Quaternion.identity);
            yield return Hold(0.5f);

            string refusal = Interactions.RunInteractionForTesting(wrench.gameObject, wrench.transform.position, "Pickup", IntentType.Help);
            Assert.IsNull(refusal, refusal);
            Assert.IsNotNull(Interactions.RunInteractionForTesting(wrench.gameObject, wrench.transform.position, "Pickup", IntentType.Help),
                "A second click while the first reaches is dropped (one pending hand action)");
            yield return Hold(0.1f);
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            ragdoll.ServerKnockdown(1f);
            yield return Hold(InteractionAnimation.All[InteractionAnimation.FloorPickup].ContactSeconds + 0.3f);

            Assert.AreNotSame(wrench, hand.ItemInHand, "An interrupted pickup leaves the item where it was");
            Assert.IsFalse(Interactions.HasPendingHandAction, "The cancelled pickup is cleared");
            yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Locomotion, "the body gets up", 8f);
            wrench.Delete();
        }

        [UnityTest]
        public IEnumerator BusyUpperBody_SkipsTheClip()
        {
            PlaceBody();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Item wrench = SubSystems.Get<ItemSubSystem>().SpawnItem(Wrench, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(wrench), "The wrench goes in the active hand");
            yield return Hold(0.3f);

            AnimationOrchestrator orchestrator = Body.GetComponent<AnimationOrchestrator>();
            Assert.IsTrue(Body.GetComponent<CombatInteractionNetwork>().TryThrowAt(Body.transform.position + new Vector3(3f, 0f, 0f)),
                "The wrench can be thrown");
            yield return Hold(0.1f);
            Assert.AreEqual(InteractionAnimation.None, orchestrator.PlayInteraction(InteractionAnimation.Reach),
                "A reach during the throw's wind-up is skipped (Decision 23)");

            yield return WaitFor(() => hand.ItemInHand != wrench, "the throw releases", 2f);
            yield return Hold(ThrowAnimation.PlaySeconds(ThrowAnimation.Overhand));
            Assert.AreEqual(InteractionAnimation.Reach, orchestrator.PlayInteraction(InteractionAnimation.Reach),
                "Once the throw is over the reach plays");
            wrench.Delete();
        }

        [UnityTest]
        public IEnumerator Defuse_KneelsForTheDelay_AndStandsWhenDone()
        {
            TimedCharge charge = PlaceArmedChargeWithCutters();
            yield return Hold(0.3f);

            HumanoidBodyStateMachine body = Body.GetComponent<HumanoidBodyStateMachine>();
            Animator animator = Body.GetComponent<Animator>();
            InteractionAnimation.Clip kneel = InteractionAnimation.All[InteractionAnimation.Kneel];
            string refusal = Interactions.RunInteractionForTesting(charge.gameObject, charge.transform.position, "DefuseC4", IntentType.Help);
            Assert.IsNull(refusal, refusal);

            yield return WaitFor(() => body.Snapshot.InteractKneel, "the defuse sets the kneel bit", 0.5f);
            yield return WaitFor(() => InState(animator, 0, kneel.StateName), "the body kneels", 1f);
            yield return Hold(TimedCharge.DefuseSeconds * 0.5f);
            Assert.IsTrue(InState(animator, 0, kneel.StateName), "It stays down while the defuse runs");

            yield return WaitFor(() => !charge.Armed, "the charge is defused", TimedCharge.DefuseSeconds + 1f);
            yield return WaitFor(() => !body.Snapshot.InteractKneel, "completion clears the kneel bit", 0.5f);
            yield return WaitFor(() => !InState(animator, 0, kneel.StateName), "the body stands", 1f);
            charge.GetComponent<Item>().Delete();
        }

        [UnityTest]
        public IEnumerator Defuse_StandsUpWhenTheBodyMoves()
        {
            TimedCharge charge = PlaceArmedChargeWithCutters();
            yield return Hold(0.3f);

            HumanoidBodyStateMachine body = Body.GetComponent<HumanoidBodyStateMachine>();
            Animator animator = Body.GetComponent<Animator>();
            InteractionAnimation.Clip kneel = InteractionAnimation.All[InteractionAnimation.Kneel];
            string refusal = Interactions.RunInteractionForTesting(charge.gameObject, charge.transform.position, "DefuseC4", IntentType.Help);
            Assert.IsNull(refusal, refusal);
            yield return WaitFor(() => InState(animator, 0, kneel.StateName), "the body kneels", 1f);

            Teleport(Body, Body.transform.position + new Vector3(0f, 0f, -0.5f));
            yield return WaitFor(() => !body.Snapshot.InteractKneel, "moving cancels the defuse and the kneel", 1f);
            yield return WaitFor(() => !InState(animator, 0, kneel.StateName), "the body stands", 1f);
            Assert.IsTrue(charge.Armed, "The cancelled defuse leaves the charge armed");
            charge.GetComponent<Item>().Delete();
        }

        private InteractionController Interactions => Body.GetComponent<InteractionController>();

        private void PlaceBody()
        {
            EmptyHands(Body);
            Teleport(Body, new Vector3(Stand.x, Body.transform.position.y, Stand.z));
        }

        /// <summary>An armed C4 on the floor in front of the body, with wirecutters in the active hand.</summary>
        private TimedCharge PlaceArmedChargeWithCutters()
        {
            PlaceBody();
            Vector3 front = Body.transform.position + Body.transform.forward * 0.7f + Vector3.up * 0.1f;
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(C4, front, Quaternion.identity);
            TimedCharge charge = item.GetComponent<TimedCharge>();
            typeof(TimedCharge).GetMethod("Arm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(charge, null);
            Assert.IsTrue(charge.Armed, "The C4 is armed");

            Item cutters = SubSystems.Get<ItemSubSystem>().SpawnItem(Wirecutters, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(Body.GetComponent<Hands>().SelectedHand.Container.AddItem(cutters), "The wirecutters go in the active hand");
            return charge;
        }

        private const string Wirecutters = "41a1c8c211dc32c4d8ffca5f0892f9dc";

        private static bool InState(Animator animator, int layer, string state) =>
            animator.GetCurrentAnimatorStateInfo(layer).IsName(state)
            || (animator.IsInTransition(layer) && animator.GetNextAnimatorStateInfo(layer).IsName(state));
    }
}
