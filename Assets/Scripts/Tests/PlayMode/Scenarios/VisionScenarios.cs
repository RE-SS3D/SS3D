using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Vision;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>What the FOV rays and wall rule do against the real wall and airlock prefabs (fov.md §2–§3).</summary>
    [Category(TestCategories.Scenarios)]
    [Category("Vision")]
    public class VisionScenarios : ScenarioFixture
    {
        // Steel wall faces at eye height, from the tile centre (VisionOccluderRecipe measures 0.216).
        private const float WallFaceFromCentre = 0.216f;

        // Guards: the wall beside a door drawn whole while the room behind it stays black (VisionOccluder arms, VisionSubSystem.IsPartOfSeenWall, 2026-11_fov-rework D1) · Not cheaper because: it needs the real wall's arms, switched by its real connections, and the live cast
        [UnityTest]
        public IEnumerator HuggingAWall_HidesTheNextRoom_ShowsTheWallWhole()
        {
            VisionSubSystem vision = LiveVision();
            Vector3 door = TheDoor().transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - door.x);
            Vector3 throughWall = Vector3.left * towardMainRoom;
            const float wallLineToBody = 0.75f;
            const float besideDoor = 1f;
            Vector3 stand = new(door.x + (towardMainRoom * wallLineToBody), Body.transform.position.y, door.z + besideDoor);

            yield return StandAt(stand);

            Assert.AreEqual(wallLineToBody - WallFaceFromCentre, vision.DepthAlong(throughWall), 0.03f,
                "A ray through the wall stops on the face the wall draws, not the tile edge");

            float floor = Body.transform.position.y;
            float farFace = door.x - (towardMainRoom * WallFaceFromCentre);
            Assert.IsTrue(vision.IsVisible(new Vector3(farFace, floor + 1.2f, stand.z)), "The wall's far face is part of the wall, drawn whole");
            Assert.IsTrue(vision.IsVisible(new Vector3(door.x, floor + 2.5f, stand.z)), "So is its top");
            Assert.IsFalse(vision.IsVisible(new Vector3(door.x - (towardMainRoom * 0.4f), floor, stand.z)),
                "The floor just past the far face is the next room's");
            Assert.IsFalse(vision.IsVisible(new Vector3(door.x - (towardMainRoom * 1.5f), floor + 1f, stand.z)),
                "And the room behind is black");
        }

        // Guards: examine and other gameplay asking the FOV what shows, against real walls (VisionSubSystem.IsVisible, examine.md §2) · Not cheaper because: the answer depends on the rays stopping on real wall prefabs and on the cast pose the live system stores
        [UnityTest]
        public IEnumerator PointBehindAWall_IsHidden_UntilThePlayerIsInThatRoom()
        {
            VisionSubSystem vision = LiveVision();
            Vector3 door = TheDoor().transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - door.x);
            float towardSideRoom = -towardMainRoom;
            const float besideDoor = 1f;
            Vector3 mainRoomFloor = new(door.x + towardMainRoom, 0f, door.z + besideDoor);
            Vector3 sideRoomFloor = new(door.x + towardSideRoom, 0f, door.z + besideDoor);
            float bodyHeight = Body.transform.position.y;

            yield return StandAt(new Vector3(mainRoomFloor.x, bodyHeight, mainRoomFloor.z));
            Assert.IsTrue(vision.IsVisible(mainRoomFloor + Vector3.forward), "The floor beside the player shows");
            Assert.IsFalse(vision.IsVisible(sideRoomFloor), "A wall hides the room behind it");

            yield return StandAt(new Vector3(sideRoomFloor.x, bodyHeight, sideRoomFloor.z));
            Assert.IsTrue(vision.IsVisible(sideRoomFloor + Vector3.forward), "It shows once the player stands there");
            Assert.IsFalse(vision.IsVisible(mainRoomFloor), "And the first room is hidden in turn");
        }

        // Guards: closed airlocks block sight whatever their access and an opening one reveals the room (fov.md §2, the leaves' VisionOccluder boxes) · Not cheaper because: the leaves move by the real door animation and their colliders follow on the physics step
        [UnityTest]
        public IEnumerator OpeningAnAirlock_RevealsTheRoomBehindIt()
        {
            VisionSubSystem vision = LiveVision();
            AirLockOpener door = TheDoor();
            Vector3 doorPosition = door.transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - doorPosition.x);
            float floor = Body.transform.position.y;
            const float inFrontOfTheDoor = 1.8f;
            Vector3 behindTheDoor = new(doorPosition.x - (towardMainRoom * 2f), floor + 0.5f, doorPosition.z);

            yield return StandAt(new Vector3(doorPosition.x + (towardMainRoom * inFrontOfTheDoor), floor, doorPosition.z));
            Assume.That(door.IsOpen, Is.False, "The door starts closed");
            Assert.IsFalse(vision.IsVisible(behindTheDoor), "A closed airlock hides the room behind it");
            Assert.IsTrue(vision.IsVisible(new Vector3(doorPosition.x, floor + 1.5f, doorPosition.z)), "The door itself shows");

            try
            {
                Assert.IsTrue(door.ServerDebugHoldOpen(), "The door has power to open");
                yield return WaitFor(() => door.IsOpen && !door.PanelsInMotion, "the door finishes opening", 5f);
                yield return new WaitForFixedUpdate();
                yield return Hold(0.2f);

                Assert.IsTrue(vision.IsVisible(behindTheDoor), "The room shows through the open doorway");
            }
            finally
            {
                door.ServerCloseFromInteraction();
            }

            yield return WaitFor(() => !door.IsOpen && !door.PanelsInMotion, "the door closes again", 5f);
        }

        private static VisionSubSystem LiveVision()
        {
            VisionSubSystem vision = SubSystems.Get<VisionSubSystem>();
            Assume.That(vision != null && vision.viewPoints.IsCreated, "FOV needs a graphics device");
            Assert.AreEqual(VisionState.Active, vision.State, "A living body sees through the FOV");
            Assert.GreaterOrEqual(VisionOccluder.Layer, 0, "The sight layer exists");
            return vision;
        }

        private static AirLockOpener TheDoor() =>
            Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Single();

        private IEnumerator StandAt(Vector3 position)
        {
            Teleport(Body, position);
            yield return Hold(0.4f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
        }
    }
}
