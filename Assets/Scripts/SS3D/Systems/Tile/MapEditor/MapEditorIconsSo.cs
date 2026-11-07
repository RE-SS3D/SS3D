using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Toolbar and library icon paths for the map editor UI.
    /// Icons are SVG assets imported as UI Toolkit <see cref="VectorImage"/>s.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/Map Editor/Icons", fileName = "MapEditorIcons")]
    public sealed class MapEditorIconsSo : ScriptableObject
    {
        public static class Paths
        {
            private const string Root = "Assets/Art/Icons/map-editor/";

            public const string Select = Root + "select.svg";
            public const string Construct = Root + "construct.svg";
            public const string Move = Root + "move.svg";
            public const string Dropper = Root + "dropper.svg";
            public const string Delete = Root + "trash.svg";
            public const string Undo = Root + "undo.svg";
            public const string Redo = Root + "redo.svg";
            public const string SaveMap = Root + "saveAs.svg";
            public const string OpenMap = Root + "openMap.svg";
            public const string RotateLeft = Root + "rotateLeft.svg";
            public const string RotateRight = Root + "rotateRight.svg";
            public const string ZoomIn = Root + "zoomIn.svg";
            public const string ZoomOut = Root + "zoomOut.svg";
            public const string PanelExpand = Root + "chevronUp.svg";
            public const string PanelCollapse = Root + "chevronDown.svg";
            public const string ResetView = Root + "resetPosition.svg";
            public const string Layers = Root + "layers.svg";
            public const string EyeOff = Root + "eyeOff.svg";
            public const string Camera = Root + "camera.svg";
            public const string Settings = Root + "settings.svg";
            public const string Search = Root + "search.svg";
            public const string ModeUpper = Root + "chevronUp.svg";
            public const string ModeLower = Root + "chevronDown.svg";
            public const string ModeItems = Root + "box.svg";
            public const string ModeScripting = Root + "script.svg";

            // Spawn Points library slots (game-icons.net glyphs, tinted per spawn kind in USS).
            private const string External = "Assets/Art/Icons/External/";
            public const string SpawnAssistant = External + "delapouite/person.svg";
            public const string SpawnSecurity = External + "delapouite/police-officer-head.svg";
            public const string SpawnTraitor = External + "delapouite/spy.svg";
            public const string SpawnMalfunctioningAI = External + "lorc/android-mask.svg";
            public const string SpawnNuclearOperatives = External + "lorc/radioactive.svg";
        }

        [Header("Baked references (filled by SS3D/Map Editor/Bake Icons for player builds)")]
        [SerializeField] private VectorImage _select;
        [SerializeField] private VectorImage _construct;
        [SerializeField] private VectorImage _move;
        [SerializeField] private VectorImage _dropper;
        [SerializeField] private VectorImage _delete;
        [SerializeField] private VectorImage _undo;
        [SerializeField] private VectorImage _redo;
        [SerializeField] private VectorImage _saveMap;
        [SerializeField] private VectorImage _openMap;
        [SerializeField] private VectorImage _rotateLeft;
        [SerializeField] private VectorImage _rotateRight;
        [SerializeField] private VectorImage _zoomIn;
        [SerializeField] private VectorImage _zoomOut;
        [SerializeField] private VectorImage _panelExpand;
        [SerializeField] private VectorImage _panelCollapse;
        [SerializeField] private VectorImage _resetView;
        [SerializeField] private VectorImage _layers;
        [SerializeField] private VectorImage _eyeOff;
        [SerializeField] private VectorImage _camera;
        [SerializeField] private VectorImage _settings;
        [SerializeField] private VectorImage _search;
        [SerializeField] private VectorImage _modeUpper;
        [SerializeField] private VectorImage _modeLower;
        [SerializeField] private VectorImage _modeItems;
        [SerializeField] private VectorImage _modeScripting;
        /// <summary>One per distinct <see cref="MapEditorSubTab.IconPath"/>; the baker fills it from the sub-tab table.</summary>
        [SerializeField] private List<BakedIcon> _subTabIcons = new();
        [SerializeField] private VectorImage _spawnAssistant;
        [SerializeField] private VectorImage _spawnSecurity;
        [SerializeField] private VectorImage _spawnTraitor;
        [SerializeField] private VectorImage _spawnMalfunctioningAI;
        [SerializeField] private VectorImage _spawnNuclearOperatives;

        public VectorImage GetToolIcon(MapEditorTool tool) =>
            tool switch
            {
                MapEditorTool.Select => Resolve(_select, Paths.Select),
                MapEditorTool.Edit => Resolve(_construct, Paths.Construct),
                MapEditorTool.Move => Resolve(_move, Paths.Move),
                MapEditorTool.Dropper => Resolve(_dropper, Paths.Dropper),
                MapEditorTool.Delete => Resolve(_delete, Paths.Delete),

                _ => null,
            };

        public VectorImage GetModeIcon(MapEditorMode mode) =>
            mode switch
            {
                MapEditorMode.Upper => Resolve(_modeUpper, Paths.ModeUpper),
                MapEditorMode.Lower => Resolve(_modeLower, Paths.ModeLower),
                MapEditorMode.Items => Resolve(_modeItems, Paths.ModeItems),
                MapEditorMode.Scripting => Resolve(_modeScripting, Paths.ModeScripting),
                _ => null,
            };

        public VectorImage GetSubcategoryIcon(MapEditorSubcategory subcategory) =>
            MapEditorSubTabs.TryGet(subcategory, out MapEditorSubTab row) ? ResolveBaked(row.IconPath) : null;

        /// <summary>Library icon for a spawn catalog key; jobs without their own glyph fall back to the map pin.</summary>
        public VectorImage GetSpawnIcon(string spawnAssetName)
        {
            if (!MapEditorSpawnCatalog.TryDecode(spawnAssetName, out SpawnPoints.SpawnPointKind kind, out string jobName,
                    out SpawnPoints.AntagonistSpawnCategory category))
                return null;

            if (kind == SpawnPoints.SpawnPointKind.Antagonist)
                return category switch
                {
                    SpawnPoints.AntagonistSpawnCategory.Traitor => Resolve(_spawnTraitor, Paths.SpawnTraitor),
                    SpawnPoints.AntagonistSpawnCategory.MalfunctioningAI => Resolve(_spawnMalfunctioningAI, Paths.SpawnMalfunctioningAI),
                    SpawnPoints.AntagonistSpawnCategory.NuclearOperatives => Resolve(_spawnNuclearOperatives, Paths.SpawnNuclearOperatives),
                    _ => GetSubcategoryIcon(MapEditorSubcategory.SpawnPoints),
                };

            return jobName switch
            {
                "Assistant" => Resolve(_spawnAssistant, Paths.SpawnAssistant),
                "Security" => Resolve(_spawnSecurity, Paths.SpawnSecurity),
                _ => GetSubcategoryIcon(MapEditorSubcategory.SpawnPoints),
            };
        }

        public VectorImage Undo => Resolve(_undo, Paths.Undo);
        public VectorImage Redo => Resolve(_redo, Paths.Redo);
        public VectorImage SaveMap => Resolve(_saveMap, Paths.SaveMap);
        public VectorImage OpenMap => Resolve(_openMap, Paths.OpenMap);
        public VectorImage RotateLeft => Resolve(_rotateLeft, Paths.RotateLeft);
        public VectorImage RotateRight => Resolve(_rotateRight, Paths.RotateRight);
        public VectorImage ZoomIn => Resolve(_zoomIn, Paths.ZoomIn);
        public VectorImage ZoomOut => Resolve(_zoomOut, Paths.ZoomOut);
        public VectorImage PanelExpand => Resolve(_panelExpand, Paths.PanelExpand);
        public VectorImage PanelCollapse => Resolve(_panelCollapse, Paths.PanelCollapse);
        public VectorImage ResetView => Resolve(_resetView, Paths.ResetView);
        public VectorImage Layers => Resolve(_layers, Paths.Layers);
        public VectorImage Camera => Resolve(_camera, Paths.Camera);
        public VectorImage Settings => Resolve(_settings, Paths.Settings);
        public VectorImage Search => Resolve(_search, Paths.Search);

        /// <summary>
        /// Icon for the floating reveal button, visible only while the HUD is hidden (toggled via
        /// the F7 hotkey — the toolbar no longer has an explicit Hide UI button).
        /// </summary>
        public VectorImage ShowUi => Resolve(_eyeOff, Paths.EyeOff);

        private VectorImage ResolveBaked(string path)
        {
            foreach (BakedIcon icon in _subTabIcons)
            {
                if (icon.Image != null && string.Equals(icon.Path, path, StringComparison.Ordinal))
                    return icon.Image;
            }

            return MapEditorIconLoader.Load(path);
        }

        [Serializable]
        public struct BakedIcon
        {
            public string Path;
            public VectorImage Image;
        }

        private static VectorImage Resolve(VectorImage baked, string path) =>
            baked != null ? baked : MapEditorIconLoader.Load(path);
    }
}
