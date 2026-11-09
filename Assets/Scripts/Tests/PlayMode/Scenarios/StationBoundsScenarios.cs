using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// Nothing leaves the station through its ceiling or its deck (#193, #152): the ceiling on every floor tile stops
    /// items and bodies under wall height, and the server puts back anything it still finds above it or below the deck.
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Environment)]
    public class StationBoundsScenarios : ScenarioFixture
    {
        private const float LaunchSpeed = 20f;

        private static readonly Regex RescueWarning = new("back into the station");

        // Guards: a loose item thrown straight up inside a room is stopped by the ceiling and never rests on a wall (StationBounds, Plenum.prefab Ceiling) · Not cheaper because: needs server item physics against the floor tiles' ceiling colliders
        [UnityTest]
        public IEnumerator Item_LaunchedUpward_StopsUnderCeiling()
        {
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(Items.Wrench, FreeFloor() + (Vector3.up * 0.2f), Quaternion.identity);
            Rigidbody body = item.GetComponent<Rigidbody>();
            yield return Hold(0.5f);

            body.linearVelocity = Vector3.up * LaunchSpeed;
            float highest = float.MinValue;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, body.position.y);
                yield return null;
            }

            Assert.Less(highest, StationBounds.EscapedAbove, "The ceiling stops the item");
            Assert.Less(body.position.y, 0.5f, "The item falls back to the deck");
        }

        // Guards: a collapsed body thrown upwards stays under the ceiling (the #193 wind-ragdoll escape) · Not cheaper because: needs owner-simulated ragdoll bones against the ceiling colliders
        [UnityTest]
        public IEnumerator CollapsedBody_LaunchedUpward_StopsUnderCeiling()
        {
            MakeCritical(Body);
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            yield return WaitFor(() => ragdoll.IsKnockedDown, "the critical body collapses");
            yield return Hold(0.5f);

            Rigidbody[] bones = Body.GetComponentsInChildren<RagdollPart>(true).Select(part => part.GetComponent<Rigidbody>()).ToArray();
            foreach (Rigidbody bone in bones)
            {
                bone.linearVelocity = Vector3.up * LaunchSpeed;
            }

            float highest = float.MinValue;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, bones.Max(bone => bone.position.y));
                yield return null;
            }

            Assert.Less(highest, StationBounds.EscapedAbove, "The ceiling stops the body");
        }

        // Guards: the server puts a loose item found above the ceiling or below the deck back on the floor, and says so (StationBoundsRescue) · Not cheaper because: needs the server's item tick and the tile map's floor lookup
        [UnityTest]
        public IEnumerator Item_OutsideStation_IsPutBackOnFloor()
        {
            Vector3 floor = FreeFloor();
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(Items.Wrench, floor + (Vector3.up * 0.2f), Quaternion.identity);
            Rigidbody body = item.GetComponent<Rigidbody>();
            yield return Hold(0.5f);

            foreach (float height in new[] { 6f, -4f })
            {
                LogAssert.Expect(LogType.Warning, RescueWarning);
                ItemUtility.SetWorldPose(item.transform, floor + (Vector3.up * height), Quaternion.identity);
                body.linearVelocity = Vector3.zero;
                yield return WaitFor(() => body.position.y > StationBounds.FloorLimit && body.position.y < 1f,
                    $"the item put {height} m off the deck is back on the floor", 2f);
            }
        }

        // Guards: the server puts a standing body found above the ceiling or below the deck back on the floor, on its owner too (Ragdoll.ServerKeepInStation) · Not cheaper because: needs the owner's CharacterController and the server's body tick together
        [UnityTest]
        public IEnumerator Body_OutsideStation_IsPutBackOnFloor()
        {
            Vector3 deck = Body.transform.position;
            foreach (float height in new[] { 6f, -4f })
            {
                LogAssert.Expect(LogType.Warning, RescueWarning);
                Teleport(Body, deck + (Vector3.up * height));
                yield return WaitFor(() => Mathf.Abs(Body.transform.position.y - deck.y) < 0.3f,
                    $"the body put {height} m off the deck is back on the floor", 2f);
                yield return Hold(0.5f);
                Assert.AreEqual(deck.y, Body.transform.position.y, 0.3f, "It stays on the floor");
            }
        }

        /// <summary>A main-room floor tile away from the body.</summary>
        private Vector3 FreeFloor()
        {
            RectInt room = ScenarioAtmos.MainRoom;
            Vector3 corner = new(room.xMin + 1, 0f, room.yMin + 1);
            Vector3 opposite = new(room.xMax - 2, 0f, room.yMax - 2);
            Vector3 body = Body.transform.position;
            body.y = 0f;
            return Vector3.Distance(corner, body) > 1f ? corner : opposite;
        }
    }
}
