#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// SS3D → Inventory → Item Pose Editor: click through the Item prefabs, see each one as an icon, in both hands
    /// and on the floor, and fix a wrong pose with ±step buttons that write the prefab
    /// (Documents/architecture/systems/inventory.md). Previews come from <see cref="ItemPoseRenderer"/>, the same code
    /// the contact sheet and the game use; edits go through <see cref="ItemPoseEdit"/>.
    /// </summary>
    public sealed class ItemPoseEditorWindow : EditorWindow
    {
        private const int PreviewSize = 170;
        private const float ListWidth = 230f;

        private static readonly string[] StepLabels = { "90°", "45°", "15°", "5°" };
        private static readonly float[] Steps = { 90f, 45f, 15f, 5f };

        private static readonly (string Label, Vector3 Axis)[] Axes =
        {
            ("X", Vector3.right),
            ("Y", Vector3.up),
            ("Z", Vector3.forward),
        };

        private enum View
        {
            Icon,
            Floor,
            FloorSide,
            RightHand,
            LeftHand,
        }

        private static readonly string[] ViewTitles = { "Icon", "Floor", "Floor, level side view", "Right hand", "Left hand" };

        private readonly List<string> _paths = new();
        private readonly List<string> _names = new();
        private readonly List<int> _filtered = new();
        private readonly Texture2D[] _views = new Texture2D[5];
        private readonly Stack<(string Path, string Label, System.Action<Item> Undo)> _undo = new();

        private string _search = string.Empty;
        private int _selected = -1;
        private int _stepIndex;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _status;
        private string _renderError;
        private string _restProblem;

        // Not restored across a domain reload: the textures it refers to are gone, OnEnable queues a fresh render.
        [System.NonSerialized] private bool _renderQueued;

        [MenuItem("SS3D/Inventory/Item Pose Editor")]
        public static void Open()
        {
            ItemPoseEditorWindow window = GetWindow<ItemPoseEditorWindow>("Item Pose Editor");
            window.minSize = new Vector2(ListWidth + (3 * PreviewSize) + 80f, 720f);
        }

        private string SelectedPath => _selected >= 0 && _selected < _paths.Count ? _paths[_selected] : null;

        private void OnEnable()
        {
            RefreshList();
            if (!SelectFromProjectSelection() && _selected < 0 && _paths.Count > 0)
            {
                Select(0);
            }
            else
            {
                QueueRender();
            }
        }

        private void OnDisable() => ClearViews();

        private void OnSelectionChange()
        {
            if (SelectFromProjectSelection())
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            HandleKeys();
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawDetail();
            }
        }

        private void HandleKeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
            {
                return;
            }

            if (e.keyCode is KeyCode.DownArrow or KeyCode.PageDown)
            {
                Step(1);
                e.Use();
            }
            else if (e.keyCode is KeyCode.UpArrow or KeyCode.PageUp)
            {
                Step(-1);
                e.Use();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(ListWidth - 8f));
                if (search != _search)
                {
                    _search = search;
                    Filter();
                }

                if (GUILayout.Button("◀ Prev", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Step(-1);
                }

                if (GUILayout.Button("Next ▶", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Step(1);
                }

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_undo.Count == 0))
                {
                    if (GUILayout.Button($"Undo ({_undo.Count})", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        UndoLast();
                    }
                }

                if (GUILayout.Button("Re-render", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    QueueRender();
                }

                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    string path = SelectedPath;
                    RefreshList();
                    Select(Mathf.Max(0, _paths.IndexOf(path)));
                }
            }
        }

        private void DrawList()
        {
            using EditorGUILayout.ScrollViewScope scroll = new(_listScroll, GUILayout.Width(ListWidth));
            _listScroll = scroll.scrollPosition;
            foreach (int index in _filtered)
            {
                bool isSelected = index == _selected;
                if (GUILayout.Toggle(isSelected, _names[index], EditorStyles.toolbarButton) && !isSelected)
                {
                    GUIUtility.keyboardControl = 0;
                    Select(index);
                }
            }
        }

        private void DrawDetail()
        {
            using EditorGUILayout.ScrollViewScope scroll = new(_detailScroll);
            _detailScroll = scroll.scrollPosition;

            string path = SelectedPath;
            Item item = path != null ? AssetDatabase.LoadAssetAtPath<Item>(path) : null;
            if (item == null)
            {
                EditorGUILayout.HelpBox("Select an Item prefab.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    EditorGUIUtility.PingObject(item.gameObject);
                }
            }

            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_renderError))
            {
                EditorGUILayout.HelpBox(_renderError, MessageType.Warning);
            }

            DrawViewRow(View.Icon, View.Floor, View.FloorSide);
            DrawViewRow(View.RightHand, View.LeftHand);

            EditorGUILayout.Space();
            _stepIndex = GUILayout.Toolbar(_stepIndex, StepLabels, GUILayout.Width(240f));
            EditorGUILayout.Space();

            DrawPoseRow(item, ItemPose.Rest, "Rest (floor)", "world axes, Y = up");
            DrawPoseRow(item, ItemPose.Icon, "Icon", item.OverridesIconRotation ? "world axes, overridden" : "world axes, follows Rest");
            bool shared = ItemPoseEdit.LeftHandShared(item);
            DrawPoseRow(
                item,
                ItemPose.HeldRight,
                shared ? "Held, both hands" : "Held, right hand",
                "character axes: X right, Y up, Z forward" + (shared ? "; the left hand shares this grip" : string.Empty));
            DrawPoseRow(item, ItemPose.HeldLeft, "Held, left hand", "character axes: X right, Y up, Z forward");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Each click saves the prefab (FishNet may rewrite its PrefabId: commit that too). "
                + "Items already placed in saved maps keep their saved rotation. "
                + "Judge Rest from the level side view: upright and flat look alike from the diagonal one.",
                MessageType.None);
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }
        }

        private void DrawViewRow(params View[] views)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (View view in views)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(PreviewSize)))
                    {
                        EditorGUILayout.LabelField(ViewTitles[(int)view], EditorStyles.miniBoldLabel);
                        Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
                        EditorGUI.DrawRect(rect, new Color(0.23f, 0.24f, 0.27f));
                        Texture2D texture = _views[(int)view];
                        if (texture != null)
                        {
                            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                        }
                        else
                        {
                            GUI.Label(rect, _renderQueued ? "rendering…" : "no view", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                }
            }
        }

        private void DrawPoseRow(Item item, ItemPose pose, string title, string axesNote)
        {
            string reason = ItemPoseEdit.WhyNotEditable(item, pose);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(120f));
                string euler = reason == null ? Format(ItemPoseEdit.CleanEuler(ItemPoseEdit.Current(item, pose))) : "—";
                EditorGUILayout.LabelField(euler, GUILayout.Width(150f));
                using (new EditorGUI.DisabledScope(reason != null))
                {
                    float step = Steps[_stepIndex];
                    foreach ((string label, Vector3 axis) in Axes)
                    {
                        if (GUILayout.Button($"{label} −", GUILayout.Width(40f)))
                        {
                            Edit(pose, edited => ItemPoseEdit.Turn(edited, pose, axis, -step));
                        }

                        if (GUILayout.Button($"{label} +", GUILayout.Width(40f)))
                        {
                            Edit(pose, edited => ItemPoseEdit.Turn(edited, pose, axis, step));
                        }
                    }
                }

                if (pose == ItemPose.HeldLeft && item.AttachmentPoint != null)
                {
                    bool shared = ItemPoseEdit.LeftHandShared(item);
                    if (shared && GUILayout.Button("Split", GUILayout.Width(80f)))
                    {
                        bool altWasAttachmentPoint = item.AttachmentPointAlt == item.AttachmentPoint;
                        Edit(
                            "split the left-hand grip",
                            ItemPoseEdit.SplitLeftHand,
                            undo => ItemPoseEdit.JoinLeftHand(undo, altWasAttachmentPoint));
                    }
                }

                if (pose == ItemPose.Rest)
                {
                    bool upright = GUILayout.Toggle(item.RestsUpright, "Stands upright", GUILayout.Width(110f));
                    if (upright != item.RestsUpright)
                    {
                        bool was = item.RestsUpright;
                        Edit(
                            upright ? "the upright flag" : "clearing the upright flag",
                            edited => ItemPoseEdit.SetRestsUpright(edited, upright),
                            undo => ItemPoseEdit.SetRestsUpright(undo, was));
                    }
                }

                if (pose == ItemPose.Icon)
                {
                    using (new EditorGUI.DisabledScope(!item.OverridesIconRotation))
                    {
                        if (GUILayout.Button("Follow Rest", GUILayout.Width(80f)))
                        {
                            Edit(pose, ItemPoseEdit.FollowRest);
                        }
                    }
                }
            }

            EditorGUILayout.LabelField(reason ?? axesNote, EditorStyles.miniLabel);
            if (pose == ItemPose.Rest && _restProblem != null)
            {
                EditorGUILayout.HelpBox("Not lying flat: " + _restProblem, MessageType.Warning);
            }

            EditorGUILayout.Space(4f);
        }

        /// <summary>Edit one pose; undo puts back the snapshot taken just before.</summary>
        private void Edit(ItemPose pose, System.Action<Item> edit)
        {
            ItemPoseEdit.Snapshot before = default;
            Edit(
                pose.ToString(),
                item =>
                {
                    before = ItemPoseEdit.Capture(item, pose);
                    edit(item);
                },
                item => ItemPoseEdit.Restore(item, before));
        }

        private void Edit(string label, System.Action<Item> edit, System.Action<Item> undo)
        {
            string path = SelectedPath;
            bool ok = ItemPoseEdit.EditPrefab(path, edit, out string error);
            if (!ok)
            {
                _status = error;
                return;
            }

            _undo.Push((path, label, undo));
            _status = $"Saved {label} on {_names[_selected]}.";
            QueueRender();
        }

        private void UndoLast()
        {
            (string path, string label, System.Action<Item> undo) = _undo.Pop();
            bool ok = ItemPoseEdit.EditPrefab(path, undo, out string error);
            int index = _paths.IndexOf(path);
            _status = ok ? $"Undid {label} on {(index >= 0 ? _names[index] : path)}." : error;
            if (index >= 0 && index != _selected)
            {
                Select(index);
            }
            else
            {
                QueueRender();
            }
        }

        private void RefreshList()
        {
            _paths.Clear();
            _names.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ItemRestPoseRecipe.ContentRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.TryGetComponent(out Item _))
                {
                    _paths.Add(path);
                }
            }

            _paths.Sort((a, b) => string.CompareOrdinal(System.IO.Path.GetFileName(a), System.IO.Path.GetFileName(b)));
            foreach (string path in _paths)
            {
                _names.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            _selected = Mathf.Min(_selected, _paths.Count - 1);
            Filter();
        }

        private void Filter()
        {
            _filtered.Clear();
            for (int i = 0; i < _names.Count; i++)
            {
                if (_search.Length == 0 || _names[i].IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _filtered.Add(i);
                }
            }
        }

        /// <summary>Move through the filtered list, wrapping at the ends.</summary>
        private void Step(int delta)
        {
            if (_filtered.Count == 0)
            {
                return;
            }

            int position = _filtered.IndexOf(_selected);
            position = position < 0 ? 0 : (position + delta + _filtered.Count) % _filtered.Count;
            Select(_filtered[position]);
        }

        private bool SelectFromProjectSelection()
        {
            if (UnityEditor.Selection.activeObject is not GameObject selected)
            {
                return false;
            }

            int index = _paths.IndexOf(AssetDatabase.GetAssetPath(selected));
            if (index < 0 || index == _selected)
            {
                return false;
            }

            Select(index);
            return true;
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _paths.Count)
            {
                return;
            }

            _selected = index;
            _status = null;
            QueueRender();
            Repaint();
        }

        /// <summary>
        /// Render on the window's next <see cref="Update"/>, not inside OnGUI, where a camera render would break the GUI
        /// state.
        /// </summary>
        private void QueueRender()
        {
            _renderQueued = true;
            Repaint();
        }

        private void Update()
        {
            if (_renderQueued)
            {
                RenderNow();
            }
        }

        private void RenderNow()
        {
            _renderQueued = false;
            ClearViews();
            _renderError = null;
            _restProblem = null;
            string path = SelectedPath;
            Item prefab = path != null ? AssetDatabase.LoadAssetAtPath<Item>(path) : null;
            if (prefab == null)
            {
                Repaint();
                return;
            }

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                _renderError = "No graphics device: previews need a GPU.";
                Repaint();
                return;
            }

            _restProblem = ItemRestPoseAudit.Problem(prefab);
            _views[(int)View.Icon] = ItemPoseRenderer.RenderIcon(prefab);
            _views[(int)View.Floor] = ItemPoseRenderer.RenderFloor(prefab, ItemPoseRenderer.FloorView, PreviewSize);
            _views[(int)View.FloorSide] = ItemPoseRenderer.RenderFloor(prefab, ItemPoseRenderer.FloorSideView, PreviewSize);
            bool posed = ItemPoseRenderer.WithPosedHuman(
                human =>
                {
                    _views[(int)View.RightHand] = ItemPoseRenderer.RenderHeld(
                        prefab, human, human.Right, ItemPoseRenderer.RightHandView, PreviewSize);
                    _views[(int)View.LeftHand] = ItemPoseRenderer.RenderHeld(
                        prefab, human, human.Left, ItemPoseRenderer.LeftHandView, PreviewSize);
                },
                out string error);
            if (!posed)
            {
                _renderError = "Held views: " + error;
            }

            Repaint();
        }

        private void ClearViews()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                {
                    DestroyImmediate(_views[i]);
                    _views[i] = null;
                }
            }
        }

        private static string Format(Vector3 euler) => $"({euler.x:0.##}, {euler.y:0.##}, {euler.z:0.##})";
    }
}
#endif
