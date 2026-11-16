using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapImport;
using SS3D.Systems.Tile.MapImport.Dmm;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile.MapImport.Editor
{
    /// <summary>
    /// Tier-A Play Mode tool: pick a .dmm, build a structural shell, write unmapped CSV.
    /// Large maps apply via coroutine so FishNet heartbeats keep ticking.
    /// </summary>
    public sealed class MapImportWindow : EditorWindow
    {
        private string _dmmPath = string.Empty;
        private string _typeMapPath = string.Empty;
        private bool _clearMap = true;
        private bool _useBBox;
        private int _minX = 1;
        private int _minY = 1;
        private int _maxX = 64;
        private int _maxY = 64;
        private int _zFilter = 1;
        private string _lastSummary = string.Empty;
        private string _progress = string.Empty;
        private bool _importing;
        private Coroutine _importCoroutine;

        [MenuItem("SS3D/Map Import/Import DMM…")]
        public static void Open()
        {
            MapImportWindow window = GetWindow<MapImportWindow>("DMM Import");
            window.minSize = new Vector2(420, 320);
            if (string.IsNullOrEmpty(window._typeMapPath))
            {
                window._typeMapPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Tools/map_import/ss13_type_map.yaml"));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Requires Play Mode with TileSubSystem (host). Places plenum + floors/walls/windows/doors + infrastructure/furniture, then refresh adjacency. Save via Map Editor (Ctrl+Shift+S). Large maps yield so the Host session stays connected.",
                MessageType.Info);

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode before importing.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            _dmmPath = EditorGUILayout.TextField("DMM", _dmmPath);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                string picked = EditorUtility.OpenFilePanel("Select SS13 DMM", "", "dmm");
                if (!string.IsNullOrEmpty(picked))
                    _dmmPath = picked;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _typeMapPath = EditorGUILayout.TextField("Type map", _typeMapPath);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                string picked = EditorUtility.OpenFilePanel("Type map YAML", "", "yaml,yml");
                if (!string.IsNullOrEmpty(picked))
                    _typeMapPath = picked;
            }

            EditorGUILayout.EndHorizontal();

            _clearMap = EditorGUILayout.Toggle("Clear current map", _clearMap);
            _useBBox = EditorGUILayout.Toggle("Crop bbox (BYOND coords)", _useBBox);
            using (new EditorGUI.DisabledScope(!_useBBox))
            {
                EditorGUILayout.BeginHorizontal();
                _minX = EditorGUILayout.IntField("Min X", _minX);
                _minY = EditorGUILayout.IntField("Min Y", _minY);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                _maxX = EditorGUILayout.IntField("Max X", _maxX);
                _maxY = EditorGUILayout.IntField("Max Y", _maxY);
                EditorGUILayout.EndHorizontal();
            }

            _zFilter = EditorGUILayout.IntField("Z filter", _zFilter);

            using (new EditorGUI.DisabledScope(_importing || !Application.isPlaying || string.IsNullOrEmpty(_dmmPath)))
            {
                if (GUILayout.Button("Import", GUILayout.Height(32)))
                    BeginImport();
            }

            if (_importing && !string.IsNullOrEmpty(_progress))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_progress, MessageType.None);
                Repaint();
            }

            if (!string.IsNullOrEmpty(_lastSummary))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last run", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(_lastSummary, GUILayout.MinHeight(80));
            }
        }

        private void BeginImport()
        {
            if (!SubSystems.TryGet(out TileSubSystem tileSystem) || tileSystem.CurrentMap == null)
            {
                EditorUtility.DisplayDialog("DMM Import", "TileSubSystem / CurrentMap not ready.", "OK");
                return;
            }

            if (!tileSystem.IsServer)
            {
                EditorUtility.DisplayDialog("DMM Import", "Run as host/server — tile placement is server-authoritative.", "OK");
                return;
            }

            if (!File.Exists(_dmmPath))
            {
                EditorUtility.DisplayDialog("DMM Import", "DMM file not found.", "OK");
                return;
            }

            if (_importCoroutine != null)
                tileSystem.StopCoroutine(_importCoroutine);

            _importing = true;
            _progress = "Parsing DMM…";
            _lastSummary = string.Empty;
            _importCoroutine = tileSystem.StartCoroutine(ImportRoutine(tileSystem));
        }

        private IEnumerator ImportRoutine(TileSubSystem tileSystem)
        {
            string typeMap = _typeMapPath;
            if (string.IsNullOrEmpty(typeMap) || !File.Exists(typeMap))
            {
                typeMap = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Tools/map_import/ss13_type_map.yaml"));
            }

            MapImportPlan plan = null;
            string reportPath = null;
            Exception parseError = null;

            try
            {
                DmmMap dmm = DmmParser.ParseFile(_dmmPath);
                Ss13TypeMapConfig config = Ss13TypeMapYaml.ParseFile(typeMap);
                Ss13TypeMapper mapper = new Ss13TypeMapper(config);
                MapImportBBox bbox = _useBBox
                    ? new MapImportBBox(_minX, _minY, _maxX, _maxY)
                    : MapImportBBox.None;
                plan = MapImportPlanner.Build(dmm, mapper, bbox, _zFilter);
                reportPath = Path.Combine(
                    Path.GetDirectoryName(_dmmPath) ?? Application.temporaryCachePath,
                    Path.GetFileNameWithoutExtension(_dmmPath) + ".unmapped.csv");
            }
            catch (Exception ex)
            {
                parseError = ex;
            }

            if (parseError != null)
            {
                _lastSummary = parseError.ToString();
                Debug.LogException(parseError);
                EditorUtility.DisplayDialog("DMM Import failed", parseError.Message, "OK");
                _importing = false;
                _importCoroutine = null;
                _progress = string.Empty;
                yield break;
            }

            _progress = $"Placing {plan.Cells.Count} cells…";
            MapImportApplyResult apply = null;
            Exception applyError = null;

            IEnumerator applyRoutine = MapImportApplier.ApplyRoutine(
                plan,
                tileSystem.CurrentMap,
                tileSystem,
                _clearMap,
                reportPath,
                onComplete: r => apply = r,
                onProgress: msg => _progress = msg);

            while (true)
            {
                bool moved;
                try
                {
                    moved = applyRoutine.MoveNext();
                }
                catch (Exception ex)
                {
                    applyError = ex;
                    break;
                }

                if (!moved)
                    break;
                yield return applyRoutine.Current;
            }

            if (applyError != null)
            {
                _lastSummary = applyError.ToString();
                Debug.LogException(applyError);
                EditorUtility.DisplayDialog("DMM Import failed", applyError.Message, "OK");
            }
            else
            {
                _lastSummary = MapImportApplier.FormatSummary(plan, apply) +
                               "\nReport: " + reportPath +
                               "\nTop unmapped:\n" + FormatTopUnmapped(plan, 12);
                Log.Information(typeof(MapImportWindow), _lastSummary);
                Debug.Log(_lastSummary);
            }

            _importing = false;
            _importCoroutine = null;
            _progress = string.Empty;
        }

        private static string FormatTopUnmapped(MapImportPlan plan, int take)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (KeyValuePair<string, int> pair in System.Linq.Enumerable.OrderByDescending(plan.UnmappedCounts, p => p.Value))
            {
                sb.Append(pair.Value).Append('\t').AppendLine(pair.Key);
                if (++n >= take)
                    break;
            }

            return sb.ToString();
        }
    }
}
