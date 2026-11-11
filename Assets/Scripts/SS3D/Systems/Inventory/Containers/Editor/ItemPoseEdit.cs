#if UNITY_EDITOR
using System;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>The orientation contexts <see cref="ItemPoseEdit"/> can change.</summary>
    public enum ItemPose
    {
        /// <summary>On the floor: <c>Item._restRotation</c>, kept equal to the prefab root rotation.</summary>
        Rest,

        /// <summary>The inventory icon: <c>Item._iconRotation</c>; editing it turns the icon override on.</summary>
        Icon,

        /// <summary>In the right hand: <see cref="Item.AttachmentPoint"/>'s local rotation.</summary>
        HeldRight,

        /// <summary>In the left hand: <see cref="Item.AttachmentPointAlt"/>'s local rotation.</summary>
        HeldLeft,
    }

    /// <summary>
    /// Turns an item's Rest, Icon or Held pose about a fixed axis and writes it into the prefab, for the
    /// <see cref="ItemPoseEditorWindow"/> (Documents/plans/item-orientation.plan.md C1). Rest and Icon turn about world
    /// axes; Held turns about character axes (X right, Y up, Z forward), since an <c>AttachmentPoint</c> is the item's
    /// rotation in character space in the reference pose. Held turns pivot on the grip point, which stays in the hand.
    /// </summary>
    public static class ItemPoseEdit
    {
        private const string RestField = "_restRotation";
        private const string IconField = "_iconRotation";
        private const string OverrideIconField = "_overrideIconRotation";

        /// <summary>What a pose was before an edit, so the edit can be undone.</summary>
        public readonly struct Snapshot
        {
            public readonly ItemPose Pose;
            public readonly Vector3 Euler;
            public readonly bool IconOverride;

            public Snapshot(ItemPose pose, Vector3 euler, bool iconOverride)
            {
                Pose = pose;
                Euler = euler;
                IconOverride = iconOverride;
            }
        }

        /// <summary><paramref name="current"/> turned <paramref name="degrees"/> about the fixed <paramref name="axis"/>.</summary>
        public static Quaternion Turn(Quaternion current, Vector3 axis, float degrees) =>
            Quaternion.AngleAxis(degrees, axis) * current;

        /// <summary>Euler angles without float noise: rounded to 0.01° and wrapped to [0, 360).</summary>
        public static Vector3 CleanEuler(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            return new Vector3(CleanAngle(euler.x), CleanAngle(euler.y), CleanAngle(euler.z));
        }

        /// <summary>
        /// True when the left hand holds the item by the right hand's <see cref="Item.AttachmentPoint"/>: no
        /// <see cref="Item.AttachmentPointAlt"/>, or one that is the same transform (most items). Turning the right-hand
        /// grip then turns both hands.
        /// </summary>
        public static bool LeftHandShared(Item item) =>
            item.AttachmentPointAlt == null || item.AttachmentPointAlt == item.AttachmentPoint;

        /// <summary>Why <paramref name="pose"/> cannot be edited on <paramref name="item"/>, or null if it can.</summary>
        public static string WhyNotEditable(Item item, ItemPose pose) => pose switch
        {
            ItemPose.HeldRight when item.AttachmentPoint == null => "No AttachmentPoint: the item sits at the hand's origin.",
            ItemPose.HeldLeft when item.AttachmentPointAlt == null && item.AttachmentPoint == null =>
                "No AttachmentPoint: the item sits at the hand's origin.",
            ItemPose.HeldLeft when LeftHandShared(item) =>
                "Shares the right-hand grip, so turning that turns both hands. Split to turn the left hand alone.",
            _ => null,
        };

        /// <summary>
        /// Give the left hand its own grip: a new <c>AttachmentPointAlt</c> next to the <c>AttachmentPoint</c>, at the
        /// same position and rotation, so the item looks the same until the left grip is turned.
        /// </summary>
        public static void SplitLeftHand(Item item)
        {
            if (item.AttachmentPoint == null || !LeftHandShared(item))
            {
                throw new InvalidOperationException("The left hand already has its own grip, or there is no AttachmentPoint to copy.");
            }

            Transform source = item.AttachmentPoint;
            Transform alt = new GameObject("AttachmentPointAlt").transform;
            alt.SetParent(source.parent, false);
            alt.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            alt.SetSiblingIndex(source.GetSiblingIndex() + 1);
            item.AttachmentPointAlt = alt;
        }

        /// <summary>
        /// Undo <see cref="SplitLeftHand"/>: delete the left grip and share the right one again, either through the
        /// same transform (<paramref name="altWasAttachmentPoint"/>) or with no Alt at all.
        /// </summary>
        public static void JoinLeftHand(Item item, bool altWasAttachmentPoint)
        {
            if (item.AttachmentPointAlt != null && item.AttachmentPointAlt != item.AttachmentPoint)
            {
                UnityEngine.Object.DestroyImmediate(item.AttachmentPointAlt.gameObject);
            }

            item.AttachmentPointAlt = altWasAttachmentPoint ? item.AttachmentPoint : null;
        }

        /// <summary>The pose's current rotation. Icon reads the rest pose while not overridden.</summary>
        public static Quaternion Current(Item item, ItemPose pose) => pose switch
        {
            ItemPose.Rest => item.WorldRestRotation,
            ItemPose.Icon => item.IconRotation,
            ItemPose.HeldRight => item.AttachmentPoint != null ? item.AttachmentPoint.localRotation : Quaternion.identity,
            ItemPose.HeldLeft => item.AttachmentPointAlt != null ? item.AttachmentPointAlt.localRotation : Quaternion.identity,
            _ => throw new ArgumentOutOfRangeException(nameof(pose)),
        };

        /// <remarks>
        /// Rest and Icon record the stored field (for Icon, not the rest pose it falls back to), so a restore leaves
        /// no diff.
        /// </remarks>
        public static Snapshot Capture(Item item, ItemPose pose)
        {
            Vector3 euler = pose switch
            {
                ItemPose.Rest => new SerializedObject(item).FindProperty(RestField).vector3Value,
                ItemPose.Icon => new SerializedObject(item).FindProperty(IconField).vector3Value,
                _ => Current(item, pose).eulerAngles,
            };
            return new Snapshot(pose, euler, item.OverridesIconRotation);
        }

        /// <summary>Turn <paramref name="pose"/> on an in-memory item (prefab contents or a test object).</summary>
        public static void Turn(Item item, ItemPose pose, Vector3 axis, float degrees)
        {
            if (WhyNotEditable(item, pose) is { } reason)
            {
                throw new InvalidOperationException(reason);
            }

            Set(item, pose, CleanEuler(Turn(Current(item, pose), axis, degrees)), iconOverride: true);
        }

        /// <summary>Put a pose back as <paramref name="snapshot"/> recorded it.</summary>
        public static void Restore(Item item, Snapshot snapshot) =>
            Set(item, snapshot.Pose, snapshot.Euler, snapshot.IconOverride);

        /// <summary>Turn the icon override off, so the icon follows the rest pose again.</summary>
        public static void FollowRest(Item item)
        {
            SerializedObject so = new(item);
            so.FindProperty(OverrideIconField).boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Load the prefab's contents, run <paramref name="edit"/> on its <see cref="Item"/> and save it. Refuses while
        /// the prefab is open in Prefab Mode, where the save would be overwritten or lost.
        /// </summary>
        public static bool EditPrefab(string path, Action<Item> edit, out string error)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() is { } stage && stage.assetPath == path)
            {
                error = "The prefab is open in Prefab Mode. Close it first.";
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                error = $"Could not load {path}.";
                return false;
            }

            try
            {
                Item item = root.GetComponent<Item>();
                if (item == null)
                {
                    error = $"{path} has no Item on its root.";
                    return false;
                }

                edit(item);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                error = saved ? null : $"Saving {path} failed.";
                return saved;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Set(Item item, ItemPose pose, Vector3 euler, bool iconOverride)
        {
            switch (pose)
            {
                case ItemPose.Rest:
                {
                    SerializedObject so = new(item);
                    so.FindProperty(RestField).vector3Value = euler;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    // A plain Instantiate(prefab) must still land at rest (ItemPoseTests).
                    item.transform.localRotation = Quaternion.Euler(euler);
                    break;
                }

                case ItemPose.Icon:
                {
                    SerializedObject so = new(item);
                    so.FindProperty(IconField).vector3Value = euler;
                    so.FindProperty(OverrideIconField).boolValue = iconOverride;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }

                case ItemPose.HeldRight:
                    item.AttachmentPoint.localRotation = Quaternion.Euler(euler);
                    break;

                case ItemPose.HeldLeft:
                    item.AttachmentPointAlt.localRotation = Quaternion.Euler(euler);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(pose));
            }
        }

        private static float CleanAngle(float degrees)
        {
            float rounded = Mathf.Repeat(Mathf.Round(degrees * 100f) / 100f, 360f);
            if (rounded >= 359.995f)
            {
                rounded = 0f;
            }

            // Also turns -0 into 0, so the YAML does not flip between them.
            return rounded == 0f ? 0f : Mathf.Round(rounded * 100f) / 100f;
        }
    }
}
#endif
