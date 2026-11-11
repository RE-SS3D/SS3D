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
    /// Floor and icon pose contract (Documents/plans/item-orientation.plan.md B1): the rest pose is a serialized
    /// field, so an instance reads it whatever its current rotation, and the icon uses it unless overridden.
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
        public void GetWorldFacing_ComposesYawWithRest()
        {
            Quaternion expected = Quaternion.Euler(0f, 137f, 0f) * Quaternion.Euler(Rest);

            Assert.Less(Quaternion.Angle(expected, _item.GetWorldFacing(137f)), Tolerance);
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
    }
}
