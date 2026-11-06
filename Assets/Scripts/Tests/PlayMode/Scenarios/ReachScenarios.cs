using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: #167 reach through walls and doors, without losing the door or wall mount itself; off-camera doors moving their solid panels while idle doors stay culled · Not cheaper because: the failures came from real prefab layouts (mounts inside the wall solid, door targets without a GameObject provider) that stub geometry doesn't have
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Reach)]
    public class ReachScenarios : ScenarioFixture
    {
        private Hand SelectedHand => Body.GetComponent<HumanInventory>().Hands.SelectedHand;

        [UnityTest]
        public IEnumerator Airlock_ReachableItself_BlocksBeyondUntilOpen()
        {
            AirLockOpener door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Single();
            yield return WaitFor(() => door.IsPowered, "the door's APC powers it");
            Assert.IsFalse(door.IsOpen, "The door starts closed");

            Vector3 doorPosition = door.transform.position;
            float towardMainRoom = Mathf.Sign(ScenarioAtmos.MainRoom.center.x - doorPosition.x);
            yield return StandAt(new Vector3(doorPosition.x + towardMainRoom, 0f, doorPosition.z), Vector3.left * towardMainRoom);

            Hand hand = SelectedHand;
            Vector3 beyond = new(doorPosition.x - (towardMainRoom * 0.75f), hand.InteractionOrigin.y, doorPosition.z);
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(hand, door, doorPosition + Vector3.up, Vector3.up)),
                "A shut airlock can be reached itself");
            Assert.IsFalse(InteractionExtensions.IsReachUnobstructed(hand.InteractionOrigin, beyond, null),
                "A shut airlock blocks reach past it");

            ChangesRoom = true;
            yield return Console.Expect("doordebug open", "Opened");
            yield return WaitFor(() => door.IsOpen, "the door opens", 3f);
            yield return WaitFor(() => !door.PanelsInMotion, "the door's panels settle", 3f);
            Physics.SyncTransforms();
            Assert.IsTrue(InteractionExtensions.IsReachUnobstructed(hand.InteractionOrigin, beyond, null),
                "An open airlock leaves the doorway clear");

            yield return Console.Expect("doordebug close", "Closed");
            yield return WaitFor(() => !door.IsOpen, "the door closes", 3f);
            yield return WaitFor(() => !door.PanelsInMotion, "the door's panels settle", 3f);
            Physics.SyncTransforms();
            Assert.IsFalse(InteractionExtensions.IsReachUnobstructed(hand.InteractionOrigin, beyond, null),
                "A closed airlock blocks reach again");
        }

        [UnityTest]
        public IEnumerator Airlock_IdleDoorsStayCulled()
        {
            AirLockOpener door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Single();
            Animator animator = door.GetComponent<Animator>();
            yield return WaitFor(() => door.IsPowered, "the door's APC powers it");
            yield return WaitFor(() => !door.PanelsInMotion, "the door is idle", 3f);
            Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode, "An idle door is culled");

            ChangesRoom = true;
            yield return Console.Expect("doordebug open", "Opened");
            yield return WaitFor(() => door.PanelsInMotion, "the panels start moving", 3f);
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode, "A moving door animates off-camera");

            yield return WaitFor(() => !door.PanelsInMotion, "the door's panels settle", 3f);
            Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode, "A settled door is culled again");
        }

        [UnityTest]
        public IEnumerator WallMount_ReachableFromItsFace_NotThroughTheWall()
        {
            PlacedTileObject mount = Object.FindObjectsByType<PlacedTileObject>(FindObjectsSortMode.None)
                .Where(tile => tile.Layer is TileLayer.WallMountHigh or TileLayer.WallMountLow)
                .Where(tile => tile.GetComponentInChildren<IInteractionTarget>() != null && tile.GetComponentInChildren<Collider>() != null)
                .FirstOrDefault(tile => !Physics.CheckSphere(tile.transform.position - tile.transform.forward + Vector3.up, 0.25f,
                    LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore));
            Assert.IsNotNull(mount, "The test room has a wall mount with open floor behind its wall");
            Selectable selectable = mount.GetComponentInChildren<Selectable>();
            IInteractionTarget target = mount.GetComponentInChildren<IInteractionTarget>();
            Vector3 facing = mount.transform.forward;

            yield return StandAt(mount.transform.position + (facing * 0.9f), -facing);
            Vector3 aim = mount.GetComponentInChildren<Collider>().bounds.center;
            Assert.IsTrue(InteractionExtensions.RangeCheck(PickFrom(selectable, target, facing, aim)),
                "A mount can be reached from the side it faces, with the point a camera ray gives");
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(SelectedHand, target)),
                "A mount can be reached from the side it faces with no resolved point");

            yield return StandAt(mount.transform.position - (facing * 0.9f), facing);
            Assert.IsFalse(InteractionExtensions.RangeCheck(PickFrom(selectable, target, -facing, aim)),
                "A mount can't be reached through its wall");
            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(SelectedHand, target)),
                "A mount can't be reached through its wall with no resolved point");
        }

        private InteractionEvent PickFrom(Selectable selectable, IInteractionTarget target, Vector3 side, Vector3 aim)
        {
            Vector3 eye = aim + (side * 2.5f) + (Vector3.up * 3f);
            Ray ray = new(eye, (aim - eye).normalized);
            Assert.IsTrue(SelectionTargetUtility.TryResolveInteractionPoint(ray, selectable, out Vector3 point, out Vector3 normal),
                "A camera ray resolves a point on the mount");
            return new InteractionEvent(SelectedHand, target, point, normal);
        }

        private IEnumerator StandAt(Vector3 position, Vector3 facing)
        {
            Teleport(Body, new Vector3(position.x, Body.transform.position.y, position.z));
            Body.transform.rotation = Quaternion.LookRotation(facing);
            yield return Hold(0.4f);
            Physics.SyncTransforms();
        }
    }
}
