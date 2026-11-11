using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Held-item grip contract (Documents/plans/item-orientation.plan.md Phase A): a calibrated
    /// <see cref="GripSocket"/> makes the grip a constant hand-local rotation that equals the authored
    /// character-facing grip in the reference pose.
    /// </summary>
    public class HeldItemGripTests
    {
        private const float Tolerance = 0.01f;

        private static readonly Quaternion CharacterRoot = Quaternion.Euler(0f, 137f, 0f);
        private static readonly Quaternion Attachment = Quaternion.Euler(-90f, 270f, 0f);

        // Hand display world rotations: the reference (idle) pose and two other poses a pickup can happen in.
        private static readonly Quaternion ReferenceHand = Quaternion.Euler(-52f, 211f, 84f);
        private static readonly Quaternion SwingHand = Quaternion.Euler(10f, 160f, 30f);
        private static readonly Quaternion BindPoseHand = Quaternion.Euler(0f, 227f, 90f);

        /// <summary>
        /// What an AttachmentPoint means: the item's rotation in character space in the idle pose. The pre-socket
        /// grip applied exactly this in world space at attach time, which baked in the hand's pose at that moment.
        /// </summary>
        private static Quaternion AuthoredGrip() => CharacterRoot * Attachment;

        [Test]
        public void CalibratedGrip_InTheReferencePose_EqualsTheAuthoredGrip()
        {
            Quaternion reference = HeldItemGrip.ReferenceRotation(ReferenceHand, CharacterRoot);
            Quaternion pivotWorld = ReferenceHand * HeldItemGrip.PivotLocalRotation(reference, Attachment);

            Assert.Less(
                Quaternion.Angle(AuthoredGrip(), pivotWorld),
                Tolerance,
                "Authored AttachmentPoints must keep their meaning: same result as before in the idle pose.");
        }

        [Test]
        public void CalibratedGrip_DoesNotDependOnTheHandPoseAtPickup()
        {
            Quaternion reference = HeldItemGrip.ReferenceRotation(ReferenceHand, CharacterRoot);
            Quaternion calibrated = HeldItemGrip.PivotLocalRotation(reference, Attachment);

            // Pre-socket grip: pivot world rotation fixed at attach time, so its hand-local rotation is that pose's.
            Quaternion preSocketFromSwing = Quaternion.Inverse(SwingHand)
                * AuthoredGrip();
            Quaternion preSocketFromBind = Quaternion.Inverse(BindPoseHand)
                * AuthoredGrip();

            Assert.Greater(Quaternion.Angle(preSocketFromSwing, preSocketFromBind), 10f,
                "Precondition: the pre-socket grip differs between pickups in different poses (#158).");

            // Back in the idle pose, the calibrated grip lands where the item was authored; the pre-socket one does not.
            Quaternion expected = AuthoredGrip();
            Assert.Less(Quaternion.Angle(expected, ReferenceHand * calibrated), Tolerance);
            Assert.Greater(Quaternion.Angle(expected, ReferenceHand * preSocketFromSwing), 10f);
        }

        [Test]
        public void CalibratedGrip_IgnoresWhichWayTheCharacterFacesAtPickup()
        {
            // The socket is measured once; the grip is local to the hand, so turning the whole body
            // (root and hand together) changes nothing in hand space.
            Quaternion reference = HeldItemGrip.ReferenceRotation(ReferenceHand, CharacterRoot);
            Quaternion turn = Quaternion.Euler(0f, 90f, 0f);
            Quaternion turnedReference = HeldItemGrip.ReferenceRotation(turn * ReferenceHand, turn * CharacterRoot);

            Assert.Less(Quaternion.Angle(reference, turnedReference), Tolerance);
        }

        [Test]
        public void GripSocket_IsUncalibratedUntilCalibrated()
        {
            GameObject go = new("GripSocketTest");
            try
            {
                GripSocket socket = go.AddComponent<GripSocket>();
                Assert.IsFalse(socket.IsCalibrated);

                socket.Calibrate(ReferenceHand, CharacterRoot, "test pose");

                Assert.IsTrue(socket.IsCalibrated);
                Assert.AreEqual("test pose", socket.ReferencePose);
                Assert.Less(Quaternion.Angle(
                    HeldItemGrip.ReferenceRotation(ReferenceHand, CharacterRoot), socket.ReferenceRotation), Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HumanPrefab_EveryItemDisplayHasACalibratedGripSocket()
        {
            const string path = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
            GameObject human = DB.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(human, path);

            List<string> missing = new();
            int checkedDisplays = 0;
            foreach (ContainerItemDisplay itemDisplay in human.GetComponentsInChildren<ContainerItemDisplay>(true))
            {
                foreach (Transform display in itemDisplay.attachedContainer.Displays)
                {
                    if (display == null)
                    {
                        continue;
                    }

                    checkedDisplays++;
                    if (!display.TryGetComponent(out GripSocket socket) || !socket.IsCalibrated)
                    {
                        missing.Add(display.name);
                    }
                }
            }

            Assert.Greater(checkedDisplays, 0, "Human.prefab should have hand display points.");
            Assert.IsEmpty(missing,
                "Uncalibrated display(s) hold items with an identity reference, at the wrong angle. "
                + "Run SS3D → Inventory → Calibrate Hand Grip Sockets and commit Human.prefab.");
        }
    }
}
