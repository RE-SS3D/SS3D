using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Furniture;
using SS3D.Systems.Vision;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>What the FOV rays do against the real wall and airlock prefabs.</summary>
    [Category(TestCategories.Scenarios)]
    [Category("Vision")]
    public class VisionScenarios : ScenarioFixture
    {
        // Guards: #145 FOV slack showing the far faces of a wall or door, without blacking out the wall beside a door jamb (VisionSubSystem.OccluderSlack, OcclusionNormal) · Not cheaper because: the failure came from how the real wall box and the airlock's jamb cap sit together, which the pure seams can't see
        [UnityTest]
        public IEnumerator HuggingAWall_HidesTheNextRoom_KeepsTheWallBesideADoor()
        {
            VisionSubSystem vision = SubSystems.Get<VisionSubSystem>();
            Assume.That(vision != null && vision.viewPoints.IsCreated, "FOV needs a graphics device");

            Vector3 door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Single().transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - door.x);
            Vector3 throughWall = Vector3.left * towardMainRoom;
            const float wallLineToBody = 0.75f;
            const float besideDoor = 1f;
            Vector3 stand = new(door.x + (towardMainRoom * wallLineToBody), Body.transform.position.y, door.z + besideDoor);

            Teleport(Body, stand);
            yield return Hold(0.4f);
            Physics.SyncTransforms();
            yield return null;

            Vector3 eye = vision.DetectionCenter;
            const float wallFaceFromCentre = 0.22f;
            float farWallFace = wallLineToBody + wallFaceFromCentre;
            Assert.Less(DistanceAlong(vision, eye, throughWall) + VisionSubSystem.ShaderDepthSlack, farWallFace,
                "A ray through the wall stops short of the far face it draws");

            const float radiansOblique = 45f * Mathf.Deg2Rad;
            Vector3 oblique = (throughWall * Mathf.Cos(radiansOblique)) + (Vector3.forward * Mathf.Sin(radiansOblique));
            float obliqueDepth = (DistanceAlong(vision, eye, oblique) + VisionSubSystem.ShaderDepthSlack) * Mathf.Cos(radiansOblique);
            Assert.Less(obliqueDepth, farWallFace,
                "So does an oblique ray, though its end is still inside the wall's collider");

            // In front of the door, a little off its centre: rays hit the side of the jamb cap,
            // whose normal runs along the wall, and go on through the wall beside the door.
            const float offDoorCentre = 0.3f;
            Teleport(Body, new Vector3(door.x + (towardMainRoom * wallLineToBody), Body.transform.position.y, door.z + offDoorCentre));
            yield return Hold(0.4f);
            Physics.SyncTransforms();
            yield return null;

            const float pastTheJamb = 0.74f;
            const float justPastTheFarFace = wallFaceFromCentre + 0.02f;
            Vector3 behindTheJamb = new(door.x - (towardMainRoom * justPastTheFarFace), 0f, door.z + pastTheJamb);
            Assert.IsFalse(vision.IsVisible(behindTheJamb), "Rays past the jamb cap stop before the wall's far face");

            const float radiansTowardJamb = 78f * Mathf.Deg2Rad;
            Vector3 towardJamb = (throughWall * Mathf.Cos(radiansTowardJamb)) + (Vector3.back * Mathf.Sin(radiansTowardJamb));
            const float rayToJambCap = 1.48f;
            const float fullSlack = 0.95f;
            Assert.Greater(DistanceAlong(vision, eye, towardJamb), rayToJambCap + fullSlack,
                "A ray stopped by the door jamb ends inside the next wall tile, so it keeps its slack");
        }

        // Guards: examine and other gameplay asking the FOV what shows, against real walls (VisionSubSystem.IsVisible, examine.md §2) · Not cheaper because: the answer depends on the rays stopping on real wall prefabs and on the cast pose the live system stores
        [UnityTest]
        public IEnumerator PointBehindAWall_IsHidden_UntilThePlayerIsInThatRoom()
        {
            VisionSubSystem vision = SubSystems.Get<VisionSubSystem>();
            Assume.That(vision != null && vision.viewPoints.IsCreated, "FOV needs a graphics device");

            Vector3 door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Single().transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - door.x);
            float towardSideRoom = -towardMainRoom;
            const float besideDoor = 1f;
            Vector3 mainRoomFloor = new(door.x + towardMainRoom, 0f, door.z + besideDoor);
            Vector3 sideRoomFloor = new(door.x + towardSideRoom, 0f, door.z + besideDoor);
            float bodyHeight = Body.transform.position.y;

            Teleport(Body, new Vector3(mainRoomFloor.x, bodyHeight, mainRoomFloor.z));
            yield return Hold(0.4f);
            Assert.IsTrue(vision.IsVisible(mainRoomFloor + Vector3.forward), "The floor beside the player shows");
            Assert.IsFalse(vision.IsVisible(sideRoomFloor), "A wall hides the room behind it");

            Teleport(Body, new Vector3(sideRoomFloor.x, bodyHeight, sideRoomFloor.z));
            yield return Hold(0.4f);
            Assert.IsTrue(vision.IsVisible(sideRoomFloor + Vector3.forward), "It shows once the player stands there");
            Assert.IsFalse(vision.IsVisible(mainRoomFloor), "And the first room is hidden in turn");
        }

        private static float DistanceAlong(VisionSubSystem vision, Vector3 eye, Vector3 direction)
        {
            float nearestAngle = float.MaxValue;
            float distance = 0f;
            for (int i = 0; i < vision.stepCount; i++)
            {
                Vector3 offset = vision.viewPoints[i] - eye;
                offset.y = 0f;
                float angle = Vector3.Angle(offset, direction);
                if (angle < nearestAngle)
                {
                    nearestAngle = angle;
                    distance = offset.magnitude;
                }
            }

            return distance;
        }
    }
}
