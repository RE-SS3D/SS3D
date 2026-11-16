using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Floor and icon pose contract (Documents/architecture/systems/inventory.md): the rest pose is a serialized
    /// field, so an instance reads it whatever its current rotation, and the icon uses it unless overridden (B3: the
    /// icon camera is fixed, so the icon pose alone decides which side faces it).
    /// </summary>
    public class ItemPoseTests
    {
        private const float Tolerance = 0.01f;

        private static readonly Vector3 Rest = new(-90f, 0f, 0f);
        private static readonly Vector3 Icon = new(0f, 90f, 45f);

        private GameObject _go;
        private Item _item;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ItemPoseTests");
            _item = _go.AddComponent<Item>();
            SerializedObject so = new(_item);
            so.FindProperty("_restRotation").vector3Value = Rest;
            so.FindProperty("_iconRotation").vector3Value = Icon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void WorldRestRotation_ReadsTheField_NotTheCurrentRotation()
        {
            _go.transform.rotation = Quaternion.Euler(10f, 200f, 30f);

            Assert.Less(Quaternion.Angle(Quaternion.Euler(Rest), _item.WorldRestRotation), Tolerance);
        }

        [Test]
        public void WorldRotation_ComposesYawWithRest()
        {
            Quaternion expected = Quaternion.Euler(0f, 137f, 0f) * Quaternion.Euler(Rest);

            Assert.Less(Quaternion.Angle(expected, ItemPlacement.WorldRotation(_item, 137f)), Tolerance);
        }

        [Test]
        public void WorldRotation_IgnoresTheItemsCurrentRotation()
        {
            Quaternion before = ItemPlacement.WorldRotation(_item, 45f);
            _go.transform.rotation = Quaternion.Euler(-90f, 251f, 18f);

            Assert.Less(Quaternion.Angle(before, ItemPlacement.WorldRotation(_item, 45f)), Tolerance);
        }

        [Test]
        public void WorldRotation_WithoutAPrefab_IsYawOnly()
        {
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), ItemPlacement.WorldRotation((SS3D.Systems.Tile.ItemObjectSo)null, 90f)), Tolerance);
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(-90f)]
        [TestCase(180f)]
        public void YawAtRest_InvertsWorldRotation_ForAPitchedRestPose(float yaw)
        {
            Quaternion world = ItemPlacement.WorldRotation(_item, yaw);

            Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, ItemPlacement.YawAtRest(_item, world)), Tolerance);
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(1f, 0f, 90f)]
        [TestCase(0f, -1f, 180f)]
        [TestCase(-1f, 0f, -90f)]
        [TestCase(0f, 0f, 0f)]
        public void YawOf_IsTheHeadingAngleFromWorldForward(float x, float z, float expectedYaw)
        {
            Assert.AreEqual(expectedYaw, ItemPlacement.YawOf(new Vector3(x, 0.7f, z)), Tolerance);
        }

        [Test]
        public void IconRotation_DefaultsToRest()
        {
            Assert.IsFalse(_item.OverridesIconRotation);
            Assert.Less(Quaternion.Angle(_item.WorldRestRotation, _item.IconRotation), Tolerance);
        }

        [Test]
        public void IconRotation_UsesIconFieldWhenOverridden()
        {
            SerializedObject so = new(_item);
            so.FindProperty("_overrideIconRotation").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.Less(Quaternion.Angle(Quaternion.Euler(Icon), _item.IconRotation), Tolerance);
        }

        [Test]
        public void PreviewCamera_IsFixed_WhateverThePreviewRotation()
        {
            Quaternion previous = RuntimePreviewGenerator.PreviewRotation;
            try
            {
                RuntimePreviewGenerator.PreviewRotation = Quaternion.identity;
                Quaternion unrotated = RuntimePreviewGenerator.CameraRotation;
                RuntimePreviewGenerator.PreviewRotation = Quaternion.Euler(0f, 0f, 90f);

                Assert.Less(Quaternion.Angle(unrotated, RuntimePreviewGenerator.CameraRotation), Tolerance,
                    "The icon camera must not follow the object: pose the item through Item.IconRotation.");
            }
            finally
            {
                RuntimePreviewGenerator.PreviewRotation = previous;
            }
        }

        [Test]
        public void ContentItemPrefabs_RestPoseMatchesRootRotation()
        {
            List<string> mismatched = new();
            List<string> paths = ItemRestPoseRecipe.FindItemPrefabPathsBaseFirst();
            foreach (string path in paths)
            {
                Item item = DB.LoadAssetAtPath<GameObject>(path).GetComponent<Item>();
                if (!ItemRestPoseRecipe.RestMatchesRoot(item))
                {
                    mismatched.Add($"{path} (rest {item.WorldRestRotation.eulerAngles}, root {item.transform.localEulerAngles})");
                }
            }

            Assert.Greater(paths.Count, 0, "Expected Item prefabs under " + ItemRestPoseRecipe.ContentRoot);
            Assert.IsEmpty(mismatched,
                "An Item's rest rotation must match its prefab root rotation. "
                + "Run SS3D → Inventory → Run Content Prefab Recipes and commit the prefabs.");
        }

        /// <summary>Pickable items, not body parts: their points are body sockets, and three of them have none.</summary>
        private const string PickableItemRoot = "Assets/Content/WorldObjects/Items/";

        [Test]
        public void PickableItemPrefabs_HaveAnAttachmentPoint()
        {
            List<string> missing = new();
            foreach (string path in ItemRestPoseRecipe.FindItemPrefabPathsBaseFirst())
            {
                if (path.StartsWith(PickableItemRoot) && DB.LoadAssetAtPath<Item>(path).AttachmentPoint == null)
                {
                    missing.Add(path);
                }
            }

            Assert.IsEmpty(missing, "Without an AttachmentPoint the item sits at the hand's origin at its root rotation.");
        }

        /// <summary>
        /// D2: the rest pose lies flat unless the item is flagged upright. Fix a failure in SS3D → Inventory → Item
        /// Pose Editor: turn Rest, or tick Stands upright.
        /// </summary>
        [Test]
        public void PickableItemPrefabs_LieFlatAtRest_UnlessFlaggedUpright()
        {
            List<string> standing = new();
            foreach (string path in ItemRestPoseRecipe.FindItemPrefabPathsBaseFirst())
            {
                if (path.StartsWith(PickableItemRoot) && ItemRestPoseAudit.Problem(DB.LoadAssetAtPath<Item>(path)) is { } problem)
                {
                    standing.Add($"{path}: {problem}");
                }
            }

            Assert.IsEmpty(standing, "Lay these flat or flag them upright in SS3D → Inventory → Item Pose Editor.");
        }

        [TestCase(0.41f, 0.098f, 0.085f, true)] // the screwdriver on its side: a little taller than wide
        [TestCase(0.14f, 0.25f, 0.14f, false)] // a can on its end
        [TestCase(0.9f, 0.25f, 0.06f, false)]  // a rifle standing on its grip
        [TestCase(0.3f, 0.05f, 0.2f, true)]    // a PDA face up
        public void LiesFlat_WantsTheThinnestAxisVertical(float x, float y, float z, bool flat)
        {
            Assert.AreEqual(flat, ItemRestPoseAudit.LiesFlat(new Vector3(x, y, z)));
        }
    }
}
