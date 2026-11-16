using System;
using System.Collections.Generic;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Area;
using SS3D.Systems.Audio;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    public sealed partial class MapEditorView
    {
        private Vector3 _areaPosition;
        private const string DepartmentAmbience = "Department default";

        private string _areaName, _areaDepartment, _areaAmbience;
        private ulong _areaAccess;
        /// <summary>Position, room name, department, ambience override (empty for none), access.</summary>
        public event Action<Vector3, string, string, string, ulong> AreaApplyRequested;
        public event Action<ushort> RoomPaletteSelected;
        private readonly Dictionary<ushort, string> _roomPaletteNames = new();

        private void BuildAreaPalette(VisualElement container)
        {
            int columns = GetMaxGridColumns();
            if (columns <= 0) columns = int.MaxValue;
            VisualElement row = null;
            void AddRoom(ushort id, string caption)
            {
                Button slot = new(() => RoomPaletteSelected?.Invoke(id)) { name = $"room-palette-{id}", tooltip = caption };
                slot.AddToClassList("map-editor-slot");
                slot.EnableInClassList("map-editor-slot--active", id == _vm.AreaRoomId);
                VisualElement frame = new();
                frame.AddToClassList("map-editor-slot__icon-frame");
                if (id == 0)
                {
                    slot.AddToClassList("map-editor-slot--create");
                    slot.tooltip = "Create a room — paint tiles to place it";
                    Label add = new("+") { name = "room-create-indicator", pickingMode = PickingMode.Ignore };
                    add.AddToClassList("map-editor-room-create-glyph");
                    frame.Add(add);
                }
                else
                {
                    VisualElement icon = CreateIconElement(_icons?.GetSubcategoryIcon(MapEditorSubcategory.Rooms));
                    icon.AddToClassList("map-editor-slot__icon");
                    icon.style.unityBackgroundImageTintColor = AreaDevSettings.GetColorForArea(id);
                    frame.Add(icon);
                }
                Label label = new(caption) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("map-editor-slot__label");
                label.AddToClassList("font-body");
                slot.Add(frame);
                slot.Add(label);
                if (row == null || row.childCount >= columns)
                {
                    row = new VisualElement();
                    row.AddToClassList("map-editor-grid-row");
                    container.Add(row);
                }
                row.Add(slot);
            }
            AddRoom(0, "New room");
            foreach (KeyValuePair<ushort, string> room in _roomPaletteNames) AddRoom(room.Key, room.Value);
        }

        private void SyncRoomPalette(AreaFloorVisualCache cache)
        {
            if (cache == null) return;
            bool changed = _roomPaletteNames.Count != cache.AreaNames.Count;
            if (!changed)
                foreach (KeyValuePair<ushort, string> room in cache.AreaNames)
                    if (!_roomPaletteNames.TryGetValue(room.Key, out string old) || old != cache.GetAreaName(room.Key)) { changed = true; break; }
            if (!changed) return;
            _roomPaletteNames.Clear();
            foreach (KeyValuePair<ushort, string> room in cache.AreaNames)
                _roomPaletteNames[room.Key] = cache.GetAreaName(room.Key);
            if (_vm.IsRoomCategory) RebuildGrid();
        }

        public void ShowAreaProperties(Vector3 position, string roomName, string department, ulong access, string ambience = "")
        {
            _areaPosition = position;
            _areaName = roomName;
            _areaDepartment = department;
            _areaAmbience = ambience;
            _areaAccess = access;
            OpenPopover("area");
        }

        public void CloseAreaProperties()
        {
            if (_vm.OpenPopover == "area") ClosePopover();
        }

        private VisualElement _areaLabelsLayer;
        private readonly Dictionary<ushort, Label> _areaNameLabels = new();

        public void UpdateAreaLabels(IReadOnlyDictionary<ushort, Vector3> anchors,
            AreaFloorVisualCache cache, Camera camera, bool visible)
        {
            if (_root == null) return;
            SyncRoomPalette(cache);
            if (_areaLabelsLayer == null)
            {
                _areaLabelsLayer = new VisualElement { name = "area-name-labels", pickingMode = PickingMode.Ignore };
                _areaLabelsLayer.style.position = Position.Absolute;
                _areaLabelsLayer.style.left = _areaLabelsLayer.style.top = 0;
                _areaLabelsLayer.style.right = _areaLabelsLayer.style.bottom = 0;
                _root.Insert(0, _areaLabelsLayer);
            }
            _areaLabelsLayer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || camera == null || cache == null || _root.panel == null) return;
            foreach (Label label in _areaNameLabels.Values) label.style.display = DisplayStyle.None;
            foreach (KeyValuePair<ushort, Vector3> anchor in anchors)
            {
                Vector3 screen = camera.WorldToScreenPoint(anchor.Value);
                if (screen.z <= 0 || !camera.pixelRect.Contains((Vector2)screen)) continue;
                if (!_areaNameLabels.TryGetValue(anchor.Key, out Label label))
                {
                    label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("map-editor-area-name");
                    label.style.borderBottomColor = AreaDevSettings.GetColorForArea(anchor.Key);
                    _areaNameLabels[anchor.Key] = label;
                    _areaLabelsLayer.Add(label);
                }
                label.text = cache.GetAreaName(anchor.Key);
                Vector2 point = InputInterface.ScreenToPanel(_root.panel, screen);
                label.style.left = point.x;
                label.style.top = point.y;
                label.style.display = DisplayStyle.Flex;
            }
        }

        private void BuildAreaPopover(VisualElement container)
        {
            container.AddToClassList("map-editor-area-form");
            Label help = new("Set room properties, then paint its tiles. Save the map to keep your changes.");
            help.style.whiteSpace = WhiteSpace.Normal;
            container.Add(help);
            container.Add(new Label("Room name"));
            TextField name = new() { name = "area-name", value = _areaName ?? "", maxLength = 80 };
            name.AddToClassList("map-editor-area-field");
            WireTextEntryField(name);
            container.Add(name);
            List<string> departments = new() { "" };
            departments.AddRange(AreaDepartments.All);
            container.Add(new Label("Department"));
            DropdownField department = new(departments, Math.Max(0, departments.IndexOf(_areaDepartment ?? ""))) { name = "area-department" };
            department.AddToClassList("map-editor-area-field");
            container.Add(department);
            List<string> ambiences = new() { DepartmentAmbience };
            ambiences.AddRange(AmbienceProfileCatalog.Instance.ProfileIds);
            container.Add(new Label("Ambience"));
            DropdownField ambience = new(ambiences, Math.Max(0, ambiences.IndexOf(_areaAmbience ?? ""))) { name = "area-ambience" };
            ambience.AddToClassList("map-editor-area-field");
            container.Add(ambience);
            SwitchRow useDefault = AddSwitchRow(container, "Use department access", _areaAccess == 0, null);
            useDefault.name = "area-default-access";
            Label hint = new("All selected access levels are required. Department access applies when no override is selected.");
            hint.style.whiteSpace = WhiteSpace.Normal;
            container.Add(hint);
            container.Add(new Label("Access override — scroll for all levels"));
            ScrollView levels = new() { name = "area-access-levels" };
            levels.AddToClassList("map-editor-area-access");
            ulong bits = _areaAccess;
            Dictionary<AccessLevel, SwitchRow> accessToggles = new();
            foreach (AccessLevel level in Enum.GetValues(typeof(AccessLevel)))
            {
                if (level == AccessLevel.None) continue;
                ulong flag = (ulong)level;
                SwitchRow toggle = AddSwitchRow(levels, level.ToString(), (bits & flag) != 0,
                    value => bits = value ? bits | flag : bits & ~flag);
                toggle.name = $"area-access-{level}";
                accessToggles[level] = toggle;
            }
            void RefreshAccessPreview()
            {
                ulong shown = useDefault.Value ? AreaAccessDefaults.FromParentTag(department.value).Value : bits;
                foreach (KeyValuePair<AccessLevel, SwitchRow> pair in accessToggles)
                {
                    pair.Value.SetValueWithoutNotify((shown & (ulong)pair.Key) != 0);
                    pair.Value.SetEnabled(!useDefault.Value);
                }
            }
            useDefault.ValueChanged += _ => RefreshAccessPreview();
            department.RegisterValueChangedCallback(_ => RefreshAccessPreview());
            RefreshAccessPreview();
            container.Add(levels);
            Button apply = new(() =>
            {
                if (!useDefault.Value && bits == 0)
                {
                    hint.text = "Select at least one access level, or use department access.";
                    return;
                }
                AreaApplyRequested?.Invoke(_areaPosition, name.value, department.value,
                    ambience.index == 0 ? "" : ambience.value, useDefault.Value ? 0 : bits);
            }) { text = "Apply room properties", name = "area-apply" };
            apply.AddToClassList("map-editor-action-btn");
            apply.AddToClassList("map-editor-action-btn--primary");
            container.Add(apply);

        }
    }
}
