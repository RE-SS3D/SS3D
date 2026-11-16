using Coimbra;
using SS3D.Core;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.TileMapCreator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Editor-only pin visuals for authored spawn and gas markers while the map editor is open.
    /// Gated by map-editor session and the Scripts layer-visibility toggle.
    /// </summary>
    public sealed class MapMarkerEditorView : MonoBehaviour
    {
        private static MapMarkerEditorView _instance;
        private readonly List<GameObject> _pins = new();
        private SpawnPointRegistry _boundSpawnPoints;
        private GasMarkerRegistry _boundGasMarkers;
        private bool _editorOpen;
        private bool _scriptsLayerVisible = true;

        public static MapMarkerEditorView EnsureExists()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(MapMarkerEditorView));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MapMarkerEditorView>();
            return _instance;
        }

        public static void RefreshAll()
        {
            if (_instance != null)
                _instance.Rebuild();
        }

        /// <summary>Map editor session open/closed. Pins only draw while open and Scripts is visible.</summary>
        public void SetEditorOpen(bool open)
        {
            _editorOpen = open;
            if (open)
            {
                _scriptsLayerVisible = TileLayerVisibilityService.IsGroupVisible(TileLayerCategory.Scripts);
                BindRegistry();
            }
            else
            {
                UnbindRegistry();
            }

            Rebuild();
        }

        public static void SetScriptsLayerVisible(bool visible)
        {
            MapMarkerEditorView view = EnsureExists();
            view._scriptsLayerVisible = visible;
            view.Rebuild();
        }

        private void OnDestroy()
        {
            UnbindRegistry();
            if (_instance == this)
                _instance = null;
        }

        private bool ShouldShowPins => _editorOpen && _scriptsLayerVisible;

        private void BindRegistry()
        {
            if (!SubSystems.TryGet(out TileSubSystem tile) || tile.SpawnPoints == null)
                return;

            if (_boundSpawnPoints == tile.SpawnPoints)
                return;

            UnbindRegistry();
            _boundSpawnPoints = tile.SpawnPoints;
            _boundGasMarkers = tile.GasMarkers;
            _boundSpawnPoints.Changed += Rebuild;
            _boundGasMarkers.Changed += Rebuild;
        }

        private void UnbindRegistry()
        {
            if (_boundSpawnPoints == null)
                return;

            _boundSpawnPoints.Changed -= Rebuild;
            _boundGasMarkers.Changed -= Rebuild;
            _boundSpawnPoints = null;
            _boundGasMarkers = null;
        }

        private void Rebuild()
        {
            ClearPins();
            if (!ShouldShowPins)
                return;

            if (_boundSpawnPoints == null)
                BindRegistry();

            if (_boundSpawnPoints == null)
                return;

            foreach (SpawnPointRecord record in _boundSpawnPoints.Records)
                _pins.Add(CreatePin(record.Position, ShortLabel(record), ColorFor(record), record.Direction));

            foreach (GasMarkerRecord record in _boundGasMarkers.Records)
            {
                _pins.Add(CreatePin(record.Position, MapEditorGasMarkerCatalog.PinLabel(record),
                    MapEditorGasMarkerCatalog.PinColor(record.Kind), facing: null));
            }
        }

        private void ClearPins()
        {
            for (int i = 0; i < _pins.Count; i++)
            {
                if (_pins[i] != null)
                    _pins[i].Dispose(true);
            }

            _pins.Clear();
        }

        private static GameObject CreatePin(Vector3 position, string text, Color color, Direction? facing)
        {
            var root = new GameObject($"MarkerPin_{text}");
            root.transform.position = position + Vector3.up * 0.15f;

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(root.transform, false);
            stem.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            stem.transform.localPosition = Vector3.up * 0.45f;
            UnityEngine.Object.Destroy(stem.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * 0.28f;
            head.transform.localPosition = Vector3.up * 0.95f;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            ApplyColor(stem, color);
            ApplyColor(head, color);

            // Facing chevron so Direction is readable at a glance.
            if (facing is { } direction)
            {
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = "Facing";
                arrow.transform.SetParent(root.transform, false);
                arrow.transform.localScale = new Vector3(0.08f, 0.08f, 0.35f);
                arrow.transform.localPosition = new Vector3(0f, 0.2f, 0.25f);
                UnityEngine.Object.Destroy(arrow.GetComponent<Collider>());
                ApplyColor(arrow, color);
                root.transform.rotation = Quaternion.Euler(0f, DirectionToYaw(direction), 0f);
            }

            CreateLabel(root.transform, text, color);

            return root;
        }

        private static void CreateLabel(Transform parent, string text, Color accent)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            TextMeshPro tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

            // Soft outline for contrast over bright floors.
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);

            // Accent underline bar under the text.
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Accent";
            bar.transform.SetParent(labelGo.transform, false);
            bar.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            bar.transform.localScale = new Vector3(1.6f, 0.06f, 0.06f);
            UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
            ApplyColor(bar, accent);

            labelGo.AddComponent<SpawnPinBillboard>();
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static Color ColorFor(SpawnPointRecord record) =>
            record.Kind == SpawnPointKind.Job
                ? new Color(0.25f, 0.75f, 1f, 0.95f)
                : new Color(1f, 0.35f, 0.25f, 0.95f);

        private static string ShortLabel(SpawnPointRecord record) =>
            record.Kind == SpawnPointKind.Job
                ? record.JobName
                : MapEditorSpawnCatalog.FormatAntagonist(record.AntagonistCategory);

        private static float DirectionToYaw(Direction direction) =>
            direction switch
            {
                Direction.North => 0f,
                Direction.NorthEast => 45f,
                Direction.East => 90f,
                Direction.SouthEast => 135f,
                Direction.South => 180f,
                Direction.SouthWest => 225f,
                Direction.West => 270f,
                Direction.NorthWest => 315f,
                _ => 0f,
            };

        /// <summary>Keeps world labels facing the active camera.</summary>
        private sealed class SpawnPinBillboard : MonoBehaviour
        {
            private void LateUpdate()
            {
                Camera camera = Camera.main;
                if (camera == null)
                    return;

                Vector3 toCamera = transform.position - camera.transform.position;
                if (toCamera.sqrMagnitude < 0.0001f)
                    return;

                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }
    }
}
