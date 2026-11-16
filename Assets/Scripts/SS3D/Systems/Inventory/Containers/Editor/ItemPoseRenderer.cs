#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Renders one Item prefab in the contexts its orientation matters in: the inventory icon (the real
    /// <see cref="Item.GenerateIcon"/>), held in a hand of <c>Human.prefab</c> in the calibrated reference pose (the
    /// real <see cref="ContainerItemDisplay.PlaceAtDisplay"/>), and lying on a floor at its rest pose. Shared by
    /// <see cref="ItemPoseContactSheet"/> and <see cref="ItemPoseEditorWindow"/>.
    /// Documents/architecture/systems/inventory.md.
    /// </summary>
    public static class ItemPoseRenderer
    {
        /// <summary>
        /// Character root faces +Z, right hand at +X: look at each hand from the front, a quarter turn toward its side,
        /// so the hanging arm reads as an arm and not as a leg.
        /// </summary>
        public static readonly Vector3 RightHandView = new Vector3(-0.7f, -0.3f, -1f).normalized;
        public static readonly Vector3 LeftHandView = new Vector3(0.7f, -0.3f, -1f).normalized;

        /// <summary>Diagonal view from above, as the player sees a dropped item.</summary>
        public static readonly Vector3 FloorView = new Vector3(-0.45f, -0.6f, -0.65f).normalized;

        /// <summary>
        /// Level view from the side. Judge "lies flat" from here: upright and flat look alike from the diagonal view.
        /// </summary>
        public static readonly Vector3 FloorSideView = new Vector3(-0.5f, 0f, -1f).normalized;

        /// <summary>
        /// Edge of the unrendered cube framed around the hand, so the hand and forearm show. The generator frames the
        /// cube's corners, so the view spans about 1.7 times this.
        /// </summary>
        private const float HeldFrameSize = 0.45f;

        private static readonly Color FloorColor = new(0.32f, 0.33f, 0.36f, 1f);

        public sealed class Hand
        {
            public Transform Display;
            public bool Mirrored;
            public Quaternion Reference;
        }

        /// <summary>A Human instance held in the reference pose, valid only inside <see cref="WithPosedHuman"/>.</summary>
        public sealed class PosedHuman
        {
            public Transform Root;
            public Hand Right;
            public Hand Left;
        }

        /// <summary>
        /// Instantiate <c>Human.prefab</c>, pose it in the calibrated reference pose and run <paramref name="whilePosed"/>.
        /// Render every held view inside the callback: the pose can revert once it returns. Restores the preview
        /// generator settings it changes. Returns false (and runs nothing) if the rig could not be posed.
        /// </summary>
        public static bool WithPosedHuman(Action<PosedHuman> whilePosed, out string error)
        {
            GameObject humanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandGripSocketCalibration.HumanPrefabPath);
            if (humanPrefab == null)
            {
                error = $"Missing prefab: {HandGripSocketCalibration.HumanPrefabPath}";
                return false;
            }

            using GeneratorSettingsScope settings = new();
            GameObject human = Object.Instantiate(humanPrefab);
            human.name = "ItemPoseRenderer Human";
            human.hideFlags = HideFlags.DontSave;
            try
            {
                human.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                // Skinned bounds stay at the bind pose in the Editor, so a tight frame on the held item culls the
                // posed arm and hand (they rest by the hip in idle, their stale bounds sit out at T-pose).
                foreach (SkinnedMeshRenderer skinned in human.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                }

                if (!TryFindHands(human, out Hand right, out Hand left, out error))
                {
                    return false;
                }

                PosedHuman posed = new() { Root = human.transform, Right = right, Left = left };
                return HandGripSocketCalibration.WithReferencePose(
                    human,
                    new List<Transform> { right.Display, left.Display },
                    () => whilePosed(posed),
                    out error);
            }
            finally
            {
                Object.DestroyImmediate(human);
            }
        }

        /// <summary>The icon exactly as the game generates it. Caller destroys the texture.</summary>
        public static Texture2D RenderIcon(Item prefab)
        {
            using GeneratorSettingsScope settings = new();
            Item instance = Object.Instantiate(prefab);
            instance.gameObject.hideFlags = HideFlags.DontSave;
            try
            {
                Sprite sprite = instance.GenerateIcon();
                if (sprite == null)
                {
                    return null;
                }

                Texture2D texture = sprite.texture;
                Object.DestroyImmediate(sprite);
                return texture;
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        /// <summary>The item in <paramref name="hand"/> as <see cref="ContainerItemDisplay"/> places it. Caller destroys the texture.</summary>
        public static Texture2D RenderHeld(Item prefab, PosedHuman human, Hand hand, Vector3 view, int size)
        {
            Item instance = Object.Instantiate(prefab);
            instance.gameObject.hideFlags = HideFlags.DontSave;
            Transform pivot = null;
            GameObject frame = null;
            try
            {
                instance.SetVisibility(true);
                ContainerItemDisplay.PlaceAtDisplay(instance, hand.Display, hand.Mirrored, hand.Reference);
                Transform parent = instance.transform.parent;
                pivot = parent != hand.Display ? parent : null;

                // Frame the display (item plus an unrendered cube around the hand), not just the item: a small
                // item framed alone hides the hand. Padding cannot do it, the generator clamps it to 0.25.
                frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(frame.GetComponent<Collider>());
                frame.GetComponent<Renderer>().forceRenderingOff = true;
                frame.transform.SetPositionAndRotation(hand.Display.position, Quaternion.identity);
                frame.transform.localScale = Vector3.one * HeldFrameSize;
                frame.transform.SetParent(hand.Display, true);
                return RenderScene(human.Root, hand.Display, view, 0.05f, size);
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
                if (pivot != null)
                {
                    Object.DestroyImmediate(pivot.gameObject);
                }

                if (frame != null)
                {
                    Object.DestroyImmediate(frame);
                }
            }
        }

        /// <summary>The item at its rest pose (the floor spawn rotation at yaw 0) on a floor quad. Caller destroys the texture.</summary>
        public static Texture2D RenderFloor(Item prefab, Vector3 view, int size)
        {
            GameObject root = new("ItemPoseRenderer Floor") { hideFlags = HideFlags.DontSave };
            Material floorMaterial = null;
            try
            {
                Item instance = Object.Instantiate(prefab, root.transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, ItemPlacement.WorldRotation(prefab, 0f));
                instance.SetVisibility(true);
                if (!TryRendererBounds(instance.transform, out Bounds bounds))
                {
                    return null;
                }

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.transform.SetParent(root.transform, false);
                float floorSize = Mathf.Max(bounds.size.x, bounds.size.z) * 2.5f;
                floor.transform.SetLocalPositionAndRotation(
                    new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
                floor.transform.localScale = Vector3.one * floorSize;
                Renderer floorRenderer = floor.GetComponent<Renderer>();
                floorMaterial = new Material(floorRenderer.sharedMaterial) { color = FloorColor };
                floorRenderer.sharedMaterial = floorMaterial;

                return RenderScene(root.transform, instance.transform, view, 0.2f, size);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (floorMaterial != null)
                {
                    Object.DestroyImmediate(floorMaterial);
                }
            }
        }

        /// <summary>The display under each avatar hand bone, with its calibrated reference.</summary>
        private static bool TryFindHands(GameObject human, out Hand right, out Hand left, out string error)
        {
            right = null;
            left = null;
            error = null;
            Animator animator = human.GetComponent<Animator>();
            Transform rightBone = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            Transform leftBone = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            if (rightBone == null || leftBone == null)
            {
                error = "Human.prefab needs a humanoid Animator with both hand bones.";
                return false;
            }

            foreach (ContainerItemDisplay itemDisplay in human.GetComponentsInChildren<ContainerItemDisplay>(true))
            {
                if (itemDisplay.attachedContainer == null || itemDisplay.attachedContainer.Displays == null)
                {
                    continue;
                }

                foreach (Transform display in itemDisplay.attachedContainer.Displays)
                {
                    if (display == null)
                    {
                        continue;
                    }

                    Quaternion reference = display.TryGetComponent(out GripSocket socket) && socket.IsCalibrated
                        ? socket.ReferenceRotation
                        : Quaternion.identity;
                    Hand hand = new() { Display = display, Mirrored = itemDisplay.Mirrored, Reference = reference };
                    if (right == null && display.IsChildOf(rightBone))
                    {
                        right = hand;
                    }
                    else if (left == null && display.IsChildOf(leftBone))
                    {
                        left = hand;
                    }
                }
            }

            if (right == null || left == null)
            {
                error = "Human.prefab has no ContainerItemDisplay display under one of the hand bones.";
                return false;
            }

            if (right.Display.GetComponent<GripSocket>() is not { IsCalibrated: true }
                || left.Display.GetComponent<GripSocket>() is not { IsCalibrated: true })
            {
                error = "A hand display has no calibrated GripSocket. Run SS3D → Inventory → Calibrate Hand Grip Sockets first.";
                return false;
            }

            return true;
        }

        /// <remarks>
        /// Restores the generator settings it changes: the icon calls the real <see cref="Item.GenerateIcon"/>,
        /// which must see the game's default camera direction and padding, not the previous floor view's.
        /// </remarks>
        private static Texture2D RenderScene(Transform root, Transform frame, Vector3 view, float padding, int size)
        {
            using GeneratorSettingsScope settings = new();
            RuntimePreviewGenerator.PreviewRotation = Quaternion.identity;
            RuntimePreviewGenerator.PreviewDirection = view;
            RuntimePreviewGenerator.Padding = padding;
            RuntimePreviewGenerator.BoundsTarget = frame;
            return RuntimePreviewGenerator.GenerateModelPreview(root, size, size, false);
        }

        private static bool TryRendererBounds(Transform target, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return any;
        }

        /// <summary>
        /// Saves the static preview generator settings and restores them on dispose. Also keeps textures readable
        /// while it is open, so callers can read or blit the previews.
        /// </summary>
        private sealed class GeneratorSettingsScope : IDisposable
        {
            private readonly bool _nonReadable;
            private readonly Quaternion _rotation;
            private readonly Vector3 _direction;
            private readonly float _padding;
            private readonly Transform _boundsTarget;

            public GeneratorSettingsScope()
            {
                _nonReadable = RuntimePreviewGenerator.MarkTextureNonReadable;
                _rotation = RuntimePreviewGenerator.PreviewRotation;
                _direction = RuntimePreviewGenerator.PreviewDirection;
                _padding = RuntimePreviewGenerator.Padding;
                _boundsTarget = RuntimePreviewGenerator.BoundsTarget;
                RuntimePreviewGenerator.MarkTextureNonReadable = false;
            }

            public void Dispose()
            {
                RuntimePreviewGenerator.MarkTextureNonReadable = _nonReadable;
                RuntimePreviewGenerator.PreviewRotation = _rotation;
                RuntimePreviewGenerator.PreviewDirection = _direction;
                RuntimePreviewGenerator.Padding = _padding;
                RuntimePreviewGenerator.BoundsTarget = _boundsTarget;
            }
        }
    }
}
#endif
