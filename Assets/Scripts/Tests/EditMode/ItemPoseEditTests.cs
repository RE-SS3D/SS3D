using System;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Item Pose Editor edits (Documents/plans/item-orientation.plan.md C1): turns are about fixed axes, Rest keeps the
    /// root rotation equal to it, Icon edits turn the override on, Held edits keep the grip point, and an undo
    /// snapshot puts the stored fields back exactly.
    /// </summary>
    public class ItemPoseEditTests
    {
        private const float Tolerance = 0.01f;

        private static readonly Vector3 Rest = new(270f, 0f, 0f);
        private static readonly Vector3 GripPosition = new(0.1f, -0.05f, 0.2f);
        private static readonly Vector3 Grip = new(0f, 90f, 0f);

        private GameObject _go;
        private Item _item;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ItemPoseEditTests");
            _item = _go.AddComponent<Item>();
            Transform attachmentPoint = new GameObject("AttachmentPoint").transform;
            attachmentPoint.SetParent(_go.transform, false);
            attachmentPoint.SetLocalPositionAndRotation(GripPosition, Quaternion.Euler(Grip));
            _item.AttachmentPoint = attachmentPoint;
            _go.transform.localRotation = Quaternion.Euler(Rest);
            SerializedObject so = new(_item);
            so.FindProperty("_restRotation").vector3Value = Rest;
            so.FindProperty("_iconRotation").vector3Value = new Vector3(1f, 2f, 3f);
            so.FindProperty("_overrideIconRotation").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void TurnRest_TurnsAboutTheWorldAxis_AndMovesTheRoot()
        {
            ItemPoseEdit.Turn(_item, ItemPose.Rest, Vector3.up, 90f);

            Quaternion expected = Quaternion.AngleAxis(90f, Vector3.up) * Quaternion.Euler(Rest);
            Assert.Less(Quaternion.Angle(expected, _item.WorldRestRotation), Tolerance);
            Assert.Less(Quaternion.Angle(_item.WorldRestRotation, _go.transform.localRotation), Tolerance);
            Assert.IsTrue(ItemRestPoseRecipe.RestMatchesRoot(_item));
        }

        [Test]
        public void TurnIcon_StartsFromRest_AndTurnsTheOverrideOn()
        {
            ItemPoseEdit.Turn(_item, ItemPose.Icon, Vector3.forward, -90f);

            Assert.IsTrue(_item.OverridesIconRotation);
            Quaternion expected = Quaternion.AngleAxis(-90f, Vector3.forward) * Quaternion.Euler(Rest);
            Assert.Less(Quaternion.Angle(expected, _item.IconRotation), Tolerance);
        }

        [Test]
        public void FollowRest_TurnsTheOverrideOff()
        {
            ItemPoseEdit.Turn(_item, ItemPose.Icon, Vector3.up, 90f);

            ItemPoseEdit.FollowRest(_item);

            Assert.IsFalse(_item.OverridesIconRotation);
            Assert.Less(Quaternion.Angle(_item.WorldRestRotation, _item.IconRotation), Tolerance);
        }

        [Test]
        public void TurnHeld_TurnsAboutTheCharacterAxis_AndKeepsTheGripPoint()
        {
            ItemPoseEdit.Turn(_item, ItemPose.HeldRight, Vector3.right, 90f);

            Quaternion expected = Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.Euler(Grip);
            Assert.Less(Quaternion.Angle(expected, _item.AttachmentPoint.localRotation), Tolerance);
            Assert.AreEqual(GripPosition, _item.AttachmentPoint.localPosition);
        }

        [Test]
        public void HeldLeft_WithoutAnAlt_IsNotEditable()
        {
            Assert.IsNotNull(ItemPoseEdit.WhyNotEditable(_item, ItemPose.HeldLeft));
            Assert.Throws<InvalidOperationException>(() => ItemPoseEdit.Turn(_item, ItemPose.HeldLeft, Vector3.up, 90f));
        }

        [TestCase(ItemPose.Rest)]
        [TestCase(ItemPose.Icon)]
        [TestCase(ItemPose.HeldRight)]
        public void Restore_PutsTheStoredFieldsBack(ItemPose pose)
        {
            ItemPoseEdit.Snapshot before = ItemPoseEdit.Capture(_item, pose);
            ItemPoseEdit.Turn(_item, pose, Vector3.up, 45f);

            ItemPoseEdit.Restore(_item, before);

            SerializedObject so = new(_item);
            Assert.AreEqual(Rest, so.FindProperty("_restRotation").vector3Value);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), so.FindProperty("_iconRotation").vector3Value);
            Assert.IsFalse(so.FindProperty("_overrideIconRotation").boolValue);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(Grip), _item.AttachmentPoint.localRotation), Tolerance);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(Rest), _go.transform.localRotation), Tolerance);
        }

        [Test]
        public void CleanEuler_RemovesFloatNoise()
        {
            Quaternion noisy = Quaternion.AngleAxis(90f, Vector3.up) * Quaternion.AngleAxis(-90f, Vector3.up)
                * Quaternion.Euler(0f, 270f, 90f);

            Vector3 clean = ItemPoseEdit.CleanEuler(noisy);

            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 270f, 90f), Quaternion.Euler(clean)), Tolerance);
            foreach (float angle in new[] { clean.x, clean.y, clean.z })
            {
                Assert.AreEqual(0f, angle % 90f, "{0} is not a whole quarter turn", clean);
                Assert.GreaterOrEqual(angle, 0f);
                Assert.Less(angle, 360f);
            }
        }

        [Test]
        public void CleanEuler_WrapsNearlyAFullTurnToZero()
        {
            Assert.AreEqual(Vector3.zero, ItemPoseEdit.CleanEuler(Quaternion.Euler(0f, -0.001f, 0f)));
        }
    }
}
