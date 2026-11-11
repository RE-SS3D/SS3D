using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a Security-area door refuses a body without the access and opens once its ID record is granted it (id-access CheckAccess, AirLockAccessGate, AirLockOpener proximity and click paths) · Not cheaper because: crosses area metadata from the loaded map, APC power, inventory credential lookup, console access edits and the door's server proximity pass in one host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Access)]
    public class AccessScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator SecurityDoor_DeniesWithoutAccess_OpensOnceGranted()
        {
            AirLockOpener door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).SingleOrDefault();
            Assert.IsNotNull(door, "The test room has one airlock");
            yield return WaitFor(() => door.IsPowered, "the door's APC powers it");
            Assert.IsFalse(door.IsOpen, "The door starts closed");

            HumanInventory inventory = Body.GetComponent<HumanInventory>();
            while (AccessCredentialResolver.TryResolveBoundRecord(inventory, out _, out IDCard jobCard))
                jobCard.Delete();
            yield return null;

            Teleport(Body, BesideDoor(door));
            yield return Hold(1f);
            Assert.IsFalse(door.IsOpen, "Without an ID the door stays shut when the body stands at it");
            yield return ClickDoor(door);
            yield return Hold(1f);
            Assert.IsFalse(door.IsOpen, "Without an ID clicking Open is refused");

            yield return Console.Expect("spawn IDCard hand", "spawned in hand");
            IDCard card = inventory.Hands.SelectedHand.ItemInHand.GetComponent<IDCard>();
            Assert.IsNotNull(card, "The spawned item is an ID card");
            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();
            CrewRecord record = idAccess.CreateCrewRecord("Scenario", "Assistant", Department.Civilian, AccessMask.None);
            idAccess.BindIdCard(card, record.Id);
            yield return ClickDoor(door);
            yield return Hold(1f);
            Assert.IsFalse(door.IsOpen, "An ID without Security access is refused");

            yield return Console.Expect("accessgrant me Security", "Granted");
            yield return WaitFor(() => door.IsOpen, "the door opens for the granted ID", 3f);

            // Leave the door shut for the next scenario: revoke, step back and let it close.
            yield return Console.Expect("accessrevoke me Security", "Revoked");
            Teleport(Body, BesideDoor(door, tiles: 3f));
            yield return WaitFor(() => !door.IsOpen, "the door closes once nobody authorised stands at it", 6f);
        }

        // Playtest bug "Noclipping doors": clicking Close while standing in the doorway slid the
        // panels' colliders across the body, which the character controller resolved by pushing it
        // sideways into the wall. The door now waits for the doorway to clear.
        [UnityTest]
        public IEnumerator Door_ClickedClosed_WaitsForTheBodyInTheDoorway()
        {
            AirLockOpener door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).SingleOrDefault();
            Assert.IsNotNull(door, "The test room has one airlock");
            yield return WaitFor(() => door.IsPowered, "the door's APC powers it");

            HumanInventory inventory = Body.GetComponent<HumanInventory>();
            if (!AccessCredentialResolver.TryResolveBoundRecord(inventory, out _, out _))
            {
                yield return Console.Expect("spawn IDCard hand", "spawned in hand");
                IDCard card = inventory.Hands.SelectedHand.ItemInHand.GetComponent<IDCard>();
                IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();
                CrewRecord record = idAccess.CreateCrewRecord("Scenario", "Assistant", Department.Civilian, AccessMask.None);
                idAccess.BindIdCard(card, record.Id);
            }

            yield return Console.Expect("accessgrant me Security", "Granted");
            Teleport(Body, BesideDoor(door));
            yield return WaitFor(() => door.IsOpen, "the door opens for the granted body beside it", 3f);
            yield return Hold(1.5f); // let the panels finish sliding open before stepping in
            Teleport(Body, new Vector3(door.transform.position.x, Body.transform.position.y, door.transform.position.z));
            yield return Hold(0.5f);

            yield return ClickDoor(door);
            yield return Hold(1.5f);
            Assert.IsTrue(door.IsOpen, "The door does not close on the body in its doorway");

            Teleport(Body, BesideDoor(door, tiles: 3f));
            yield return WaitFor(() => !door.IsOpen, "the clicked close goes through once the doorway is clear", 3f);

            yield return Console.Expect("accessrevoke me Security", "Revoked");
        }

        private IEnumerator ClickDoor(AirLockOpener door)
        {
            InteractionController interactions = Body.GetComponent<InteractionController>();
            string refusal = interactions.RunInteractionForTesting(door.gameObject, door.transform.position + Vector3.up,
                "OpenAirlock", IntentType.Help);
            Assert.IsNull(refusal, refusal);
            yield return null;
        }

        /// <summary>A floor point on the main room's side of the door, <paramref name="tiles"/> away from it.</summary>
        private Vector3 BesideDoor(AirLockOpener door, float tiles = 1f)
        {
            Vector3 doorPosition = door.transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - doorPosition.x);
            return new Vector3(doorPosition.x + towardMainRoom * tiles, Body.transform.position.y, doorPosition.z);
        }
    }
}
