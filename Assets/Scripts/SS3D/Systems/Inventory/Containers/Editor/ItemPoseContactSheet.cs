#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Renders every Item prefab in four views into one PNG for a quick visual review of item orientation:
    /// the inventory icon (the real <see cref="Item.GenerateIcon"/>), held in the right and left hand of
    /// <c>Human.prefab</c> in the calibrated reference pose (the real
    /// <see cref="ContainerItemDisplay.PlaceAtDisplay"/>), and lying on a floor in its rest pose. It catches
    /// upside-down and sideways cases no assertion can judge. Documents/plans/item-orientation.plan.md D1.
    /// </summary>
    /// <remarks>
    /// Writes <c>artifacts/item-poses/item-pose-contact-sheet.png</c> plus a CSV index (gitignored). Needs a
    /// GPU: run it from the Editor menu, not a <c>-nographics</c> batchmode.
    /// </remarks>
    public static class ItemPoseContactSheet
    {
        public const string OutputDirectory = "artifacts/item-poses";
        public const string SheetFileName = "item-pose-contact-sheet.png";
        public const string IndexFileName = "item-pose-contact-sheet.csv";

        private const int Cell = 128;
        private const int CellGap = 2;
        private const int BlockGap = 12;
        private const int BlocksPerRow = 4;
        private const int LabelHeight = 20;
        private const int GlyphScale = 2;

        /// <summary>Character root faces +Z, right hand at +X: look at each hand from its outer side, a little in front.</summary>
        private static readonly Vector3 RightHandView = new Vector3(-1f, -0.25f, -0.35f).normalized;
        private static readonly Vector3 LeftHandView = new Vector3(1f, -0.25f, -0.35f).normalized;
        private static readonly Vector3 FloorView = new Vector3(-0.45f, -0.6f, -0.65f).normalized;

        /// <summary>Held views frame at least this much space around the item, so the hand shows.</summary>
        private const float MinHeldFrameSize = 0.4f;

        private static readonly string[] ViewNames = { "ICON", "RIGHT HAND", "LEFT HAND", "FLOOR" };

        private static readonly Color32 SheetBackground = new(28, 30, 34, 255);
        private static readonly Color32 CellBackground = new(58, 62, 70, 255);
        private static readonly Color32 MissingCell = new(110, 40, 40, 255);
        private static readonly Color32 LabelColor = new(230, 230, 230, 255);
        private static readonly Color32 WarnColor = new(255, 170, 60, 255);
        private static readonly Color FloorColor = new(0.32f, 0.33f, 0.36f, 1f);

        [MenuItem("SS3D/Inventory/Render Item Pose Contact Sheet")]
        public static void RenderMenu()
        {
            bool ok = Render(out string report);
            if (ok)
            {
                Debug.Log("[ItemPoseContactSheet] " + report);
                EditorUtility.RevealInFinder(Path.Combine(OutputDirectory, SheetFileName));
            }
            else
            {
                Debug.LogError("[ItemPoseContactSheet] " + report);
                EditorUtility.DisplayDialog("Item Pose Contact Sheet", report, "OK");
            }
        }

        /// <summary>BatchMode (with a GPU): <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.ItemPoseContactSheet.RenderBatch</c></summary>
        public static void RenderBatch()
        {
            bool ok = Render(out string report);
            if (ok)
            {
                Debug.Log("[ItemPoseContactSheet] " + report);
            }
            else
            {
                Debug.LogError("[ItemPoseContactSheet] " + report);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        private sealed class Entry
        {
            public string Path;
            public Item Prefab;
            public readonly Texture2D[] Views = new Texture2D[4];
            public readonly List<string> Flags = new();
            public bool HasAlt;
        }

        private sealed class Hand
        {
            public Transform Display;
            public bool Mirrored;
            public Quaternion Reference;
        }

        /// <summary>Render the sheet. Returns false if the Human rig could not be posed or nothing was found.</summary>
        public static bool Render(out string report)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                report = "No graphics device (-nographics?): the contact sheet needs a GPU.";
                return false;
            }

            List<Entry> entries = FindItemPrefabs();
            if (entries.Count == 0)
            {
                report = "No Item prefabs found.";
                return false;
            }

            GameObject humanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandGripSocketCalibration.HumanPrefabPath);
            if (humanPrefab == null)
            {
                report = $"Missing prefab: {HandGripSocketCalibration.HumanPrefabPath}";
                return false;
            }

            bool previousNonReadable = RuntimePreviewGenerator.MarkTextureNonReadable;
            Quaternion previousRotation = RuntimePreviewGenerator.PreviewRotation;
            Vector3 previousDirection = RuntimePreviewGenerator.PreviewDirection;
            float previousPadding = RuntimePreviewGenerator.Padding;
            Transform previousBoundsTarget = RuntimePreviewGenerator.BoundsTarget;
            RuntimePreviewGenerator.MarkTextureNonReadable = false;

            GameObject human = Object.Instantiate(humanPrefab);
            human.name = "ItemPoseContactSheet Human";
            human.hideFlags = HideFlags.DontSave;
            string error = null;
            try
            {
                human.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (!TryFindHands(human, out Hand right, out Hand left, out error))
                {
                    report = error;
                    return false;
                }

                int index = 0;
                bool posed = HandGripSocketCalibration.WithReferencePose(
                    human,
                    new List<Transform> { right.Display, left.Display },
                    () =>
                    {
                        foreach (Entry entry in entries)
                        {
                            index++;
                            EditorUtility.DisplayProgressBar(
                                "Item Pose Contact Sheet", entry.Prefab.name, index / (float)entries.Count);
                            RenderEntry(entry, human.transform, right, left);
                        }
                    },
                    out error);
                if (!posed)
                {
                    report = error;
                    return false;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(human);
                RuntimePreviewGenerator.MarkTextureNonReadable = previousNonReadable;
                RuntimePreviewGenerator.PreviewRotation = previousRotation;
                RuntimePreviewGenerator.PreviewDirection = previousDirection;
                RuntimePreviewGenerator.Padding = previousPadding;
                RuntimePreviewGenerator.BoundsTarget = previousBoundsTarget;
            }

            try
            {
                Texture2D sheet = Compose(entries);
                Directory.CreateDirectory(OutputDirectory);
                string sheetPath = Path.Combine(OutputDirectory, SheetFileName);
                File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
                File.WriteAllText(Path.Combine(OutputDirectory, IndexFileName), Index(entries));

                int flagged = entries.FindAll(e => e.Flags.Count > 0).Count;
                report = $"{entries.Count} item prefab(s), {flagged} flagged. Wrote {Path.GetFullPath(sheetPath)}";
                return true;
            }
            finally
            {
                foreach (Entry entry in entries)
                {
                    foreach (Texture2D view in entry.Views)
                    {
                        if (view != null)
                        {
                            Object.DestroyImmediate(view);
                        }
                    }
                }
            }
        }

        private static List<Entry> FindItemPrefabs()
        {
            List<Entry> entries = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.TryGetComponent(out Item item))
                {
                    entries.Add(new Entry { Path = path, Prefab = item });
                }
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return entries;
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

        private static void RenderEntry(Entry entry, Transform human, Hand right, Hand left)
        {
            Item prefab = entry.Prefab;
            if (prefab.AttachmentPoint == null)
            {
                entry.Flags.Add("NO ATTACHMENTPOINT");
            }

            entry.HasAlt = prefab.AttachmentPointAlt != null;
            entry.Views[0] = RenderIcon(prefab);
            entry.Views[1] = RenderHeld(prefab, human, right, RightHandView);
            entry.Views[2] = RenderHeld(prefab, human, left, LeftHandView);
            entry.Views[3] = RenderFloor(prefab);
            for (int i = 0; i < entry.Views.Length; i++)
            {
                if (entry.Views[i] == null)
                {
                    entry.Flags.Add("NO " + ViewNames[i]);
                }
            }
        }

        /// <summary>The icon exactly as the game generates it.</summary>
        private static Texture2D RenderIcon(Item prefab)
        {
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

        private static Texture2D RenderHeld(Item prefab, Transform human, Hand hand, Vector3 view)
        {
            Item instance = Object.Instantiate(prefab);
            instance.gameObject.hideFlags = HideFlags.DontSave;
            Transform pivot = null;
            try
            {
                instance.SetVisibility(true);
                ContainerItemDisplay.PlaceAtDisplay(instance, hand.Display, hand.Mirrored, hand.Reference);
                Transform parent = instance.transform.parent;
                pivot = parent != hand.Display ? parent : null;

                float size = RendererSize(instance.transform);
                float padding = size > 0f ? Mathf.Max(0.35f, (MinHeldFrameSize / size - 1f) * 0.5f) : 0.35f;
                return RenderScene(human, instance.transform, Quaternion.identity, view, padding);
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
                if (pivot != null)
                {
                    Object.DestroyImmediate(pivot.gameObject);
                }
            }
        }

        /// <summary>The item at its rest pose (the floor spawn rotation at yaw 0) on a floor quad.</summary>
        private static Texture2D RenderFloor(Item prefab)
        {
            GameObject root = new("ItemPoseContactSheet Floor") { hideFlags = HideFlags.DontSave };
            Material floorMaterial = null;
            try
            {
                Item instance = Object.Instantiate(prefab, root.transform);
                // The prefab root rotation is the rest pose (WorldRestRotation of a prefab); avoid the runtime asset lookup.
                instance.transform.SetLocalPositionAndRotation(
                    Vector3.zero, Item.ComposeWorldFacing(prefab.transform.localRotation, 0f));
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

                return RenderScene(root.transform, instance.transform, Quaternion.identity, FloorView, 0.2f);
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

        private static Texture2D RenderScene(Transform root, Transform frame, Quaternion rotation, Vector3 view, float padding)
        {
            RuntimePreviewGenerator.PreviewRotation = rotation;
            RuntimePreviewGenerator.PreviewDirection = view;
            RuntimePreviewGenerator.Padding = padding;
            RuntimePreviewGenerator.BoundsTarget = frame;
            try
            {
                return RuntimePreviewGenerator.GenerateModelPreview(root, Cell, Cell, false);
            }
            finally
            {
                RuntimePreviewGenerator.BoundsTarget = null;
            }
        }

        private static float RendererSize(Transform target) =>
            TryRendererBounds(target, out Bounds bounds) ? bounds.size.magnitude : 0f;

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

        private static Texture2D Compose(List<Entry> entries)
        {
            int blockWidth = (4 * Cell) + (3 * CellGap);
            int blockHeight = LabelHeight + Cell;
            int rows = (entries.Count + BlocksPerRow - 1) / BlocksPerRow;
            int width = BlockGap + (BlocksPerRow * (blockWidth + BlockGap));
            int height = BlockGap + LabelHeight + (rows * (blockHeight + BlockGap));

            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = SheetBackground;
            }

            // Column headers once at the top of each block column.
            for (int column = 0; column < BlocksPerRow; column++)
            {
                for (int view = 0; view < 4; view++)
                {
                    int x = BlockGap + (column * (blockWidth + BlockGap)) + (view * (Cell + CellGap));
                    DrawText(pixels, width, height, x + 2, BlockGap + 3, ViewNames[view], LabelColor);
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                int bx = BlockGap + ((i % BlocksPerRow) * (blockWidth + BlockGap));
                int by = BlockGap + LabelHeight + ((i / BlocksPerRow) * (blockHeight + BlockGap));

                string label = $"{i + 1} {entry.Prefab.name}";
                if (entry.Flags.Count > 0)
                {
                    label += " " + string.Join(" ", entry.Flags);
                }

                DrawText(pixels, width, height, bx + 2, by + 3, label, entry.Flags.Exists(f => f.StartsWith("NO")) ? WarnColor : LabelColor);

                for (int view = 0; view < 4; view++)
                {
                    int cx = bx + (view * (Cell + CellGap));
                    int cy = by + LabelHeight;
                    Texture2D texture = entry.Views[view];
                    FillRect(pixels, width, height, cx, cy, Cell, Cell, texture != null ? CellBackground : MissingCell);
                    if (texture != null)
                    {
                        Blit(pixels, width, height, cx, cy, texture);
                    }
                }
            }

            Texture2D sheet = new(width, height, TextureFormat.RGBA32, false);
            sheet.SetPixels32(pixels);
            sheet.Apply(false);
            return sheet;
        }

        private static string Index(List<Entry> entries)
        {
            StringBuilder csv = new();
            csv.AppendLine("number,name,path,has_attachment_point_alt,flags");
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                csv.AppendLine($"{i + 1},{entry.Prefab.name},{entry.Path},{entry.HasAlt},{string.Join(" ", entry.Flags)}");
            }

            return csv.ToString();
        }

        // Pixel helpers. (x, y) are from the top-left of the sheet; textures are stored bottom-up.

        private static void FillRect(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    SetPixel(pixels, width, height, px, py, color);
                }
            }
        }

        private static void Blit(Color32[] pixels, int width, int height, int x, int y, Texture2D texture)
        {
            Color32[] source = texture.GetPixels32();
            int w = Mathf.Min(texture.width, Cell);
            int h = Mathf.Min(texture.height, Cell);
            bool hasAlpha = texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.ARGB32;
            for (int sy = 0; sy < h; sy++)
            {
                for (int sx = 0; sx < w; sx++)
                {
                    Color32 src = source[(sy * texture.width) + sx];
                    int px = x + sx;
                    int py = y + (h - 1 - sy);
                    if (hasAlpha && src.a < 255)
                    {
                        int index = ((height - 1 - py) * width) + px;
                        if (px < 0 || px >= width || py < 0 || py >= height)
                        {
                            continue;
                        }

                        src = Color32.Lerp(pixels[index], new Color32(src.r, src.g, src.b, 255), src.a / 255f);
                    }

                    SetPixel(pixels, width, height, px, py, src);
                }
            }
        }

        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            pixels[((height - 1 - y) * width) + x] = color;
        }

        private static void DrawText(Color32[] pixels, int width, int height, int x, int y, string text, Color32 color)
        {
            int advance = 6 * GlyphScale;
            int maxChars = ((4 * Cell) + (3 * CellGap) - 4) / advance;
            string upper = text.ToUpperInvariant();
            if (upper.Length > maxChars)
            {
                upper = upper.Substring(0, maxChars);
            }

            for (int c = 0; c < upper.Length; c++)
            {
                byte[] glyph = Glyph(upper[c]);
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if ((glyph[row] & (0x10 >> col)) == 0)
                        {
                            continue;
                        }

                        FillRect(pixels, width, height,
                            x + (c * advance) + (col * GlyphScale), y + (row * GlyphScale),
                            GlyphScale, GlyphScale, color);
                    }
                }
            }
        }

        /// <summary>5x7 bitmap glyphs, one byte per row, bit 4 = leftmost column.</summary>
        private static byte[] Glyph(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return Letters[c - 'A'];
            }

            if (c >= '0' && c <= '9')
            {
                return Digits[c - '0'];
            }

            return c switch
            {
                ' ' => new byte[7],
                '-' => new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
                '_' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
                '.' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x0C },
                '(' => new byte[] { 0x02, 0x04, 0x08, 0x08, 0x08, 0x04, 0x02 },
                ')' => new byte[] { 0x08, 0x04, 0x02, 0x02, 0x02, 0x04, 0x08 },
                '/' => new byte[] { 0x01, 0x01, 0x02, 0x04, 0x08, 0x10, 0x10 },
                ':' => new byte[] { 0x00, 0x0C, 0x0C, 0x00, 0x0C, 0x0C, 0x00 },
                '+' => new byte[] { 0x00, 0x04, 0x04, 0x1F, 0x04, 0x04, 0x00 },
                _ => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
            };
        }

        private static readonly byte[][] Letters =
        {
            new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 }, // A
            new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E }, // B
            new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E }, // C
            new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C }, // D
            new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F }, // E
            new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 }, // F
            new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F }, // G
            new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 }, // H
            new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E }, // I
            new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C }, // J
            new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 }, // K
            new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F }, // L
            new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 }, // M
            new byte[] { 0x11, 0x11, 0x19, 0x15, 0x13, 0x11, 0x11 }, // N
            new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E }, // O
            new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 }, // P
            new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D }, // Q
            new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 }, // R
            new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E }, // S
            new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 }, // T
            new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E }, // U
            new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 }, // V
            new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A }, // W
            new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 }, // X
            new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 }, // Y
            new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F }, // Z
        };

        private static readonly byte[][] Digits =
        {
            new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E }, // 0
            new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E }, // 1
            new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F }, // 2
            new byte[] { 0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E }, // 3
            new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 }, // 4
            new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E }, // 5
            new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E }, // 6
            new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 }, // 7
            new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E }, // 8
            new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C }, // 9
        };
    }
}
#endif
