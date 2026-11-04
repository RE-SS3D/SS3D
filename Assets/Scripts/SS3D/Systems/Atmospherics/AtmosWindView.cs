using Coimbra;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.Visualization;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Thin streaks that ride the atmos flow atlas around the camera, one mesh and no per-streak
    /// objects. Reads the same data as the gas shaders, so hosts and pure clients show the same wind
    /// without any wind traffic of their own, and streaks stop when the field goes still.
    /// </summary>
    public sealed class AtmosWindView : MonoBehaviour
    {
        private const int StreakCount = 64;
        private const int PointsPerStreak = 7;
        private const int VertexCount = StreakCount * PointsPerStreak * 2;
        private const int SpawnTriesPerFrame = 24;
        private const float SpawnRadius = 9f;
        private const float MinFlow = 0.22f;
        private const float MetresPerSecondAtFullFlow = 9f;
        private const float MinLife = 0.45f;
        private const float MaxLife = 0.9f;
        private const float TrailFollowRate = 22f;
        private const float HalfWidth = 0.009f;
        private const float MaxAlpha = 0.5f;

        private static readonly Color32 StreakColour = new(215, 240, 255, 0);

        private readonly Vector3[] _points = new Vector3[StreakCount * PointsPerStreak];
        private readonly float[] _age = new float[StreakCount];
        private readonly float[] _life = new float[StreakCount];
        private readonly float[] _strength = new float[StreakCount];
        private readonly Vector3[] _vertices = new Vector3[VertexCount];
        private readonly Color32[] _colours = new Color32[VertexCount];

        private Mesh _mesh;
        private Material _material;
        private MeshRenderer _renderer;
        private GameObject _meshObject;

        /// <summary>Streaks drawn last frame.</summary>
        public int VisibleStreaks { get; private set; }

        /// <summary>The published flow at a world position, or still air with no atlas.</summary>
        public Vector2 SampleFlow(float worldX, float worldZ) =>
            AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot)
                ? AtmosCellVisualEncoding.SampleFlow(snapshot.FlowData, snapshot.AtlasBounds, worldX, worldZ)
                : Vector2.zero;



        private void Awake()
        {
            Shader shader = Resources.Load<Shader>("Atmospherics/Wind");
            if (shader == null)
            {
                enabled = false;
                return;
            }

            _material = new Material(shader);
            _mesh = new Mesh { name = "Atmos wind streaks" };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colours);
            _mesh.SetTriangles(BuildTriangles(), 0);

            _meshObject = new GameObject("Atmos wind streaks");
            _meshObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _meshObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        private void Update()
        {
            if (!AtmosRenderContext.TryGetRequest(out AtmosRenderContext.Request request)
                || !AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot)
                || snapshot.FlowData == null)
            {
                _renderer.enabled = false;
                VisibleStreaks = 0;
                return;
            }

            Camera view = request.SourceCamera;
            Vector3 focus = GroundFocus(view.transform);
            float seconds = Time.deltaTime;

            int alive = 0;
            int spawnTries = SpawnTriesPerFrame;
            for (int streak = 0; streak < StreakCount; streak++)
            {
                if (_age[streak] >= _life[streak] && spawnTries > 0)
                {
                    spawnTries--;
                    TrySpawn(streak, focus, snapshot);
                }

                if (_age[streak] >= _life[streak])
                {
                    HideStreak(streak);
                    continue;
                }

                Advance(streak, seconds, snapshot);
                WriteRibbon(streak, view.transform.position);
                alive++;
            }

            VisibleStreaks = alive;
            _renderer.enabled = alive > 0;
            if (alive == 0)
                return;

            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colours);
            _mesh.bounds = new Bounds(focus, Vector3.one * (SpawnRadius * 3f));
        }

        private void OnDestroy()
        {
            if (_meshObject != null) _meshObject.Dispose(true);
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }

        private static Vector3 GroundFocus(Transform camera)
        {
            Vector3 forward = camera.forward;
            if (forward.y > -0.05f)
                return camera.position;

            return camera.position + forward * Mathf.Min(camera.position.y / -forward.y, SpawnRadius * 2f);
        }

        private void TrySpawn(int streak, Vector3 focus, in AtmosRenderContext.Snapshot snapshot)
        {
            Vector2 offset = Random.insideUnitCircle * SpawnRadius;
            float x = focus.x + offset.x;
            float z = focus.z + offset.y;
            Vector2 flow = AtmosCellVisualEncoding.SampleFlow(snapshot.FlowData, snapshot.AtlasBounds, x, z);
            float strength = flow.magnitude;
            if (strength < MinFlow || Random.value > strength)
                return;

            var start = new Vector3(x, Random.Range(0.1f, 1.2f), z);
            int first = streak * PointsPerStreak;
            for (int point = 0; point < PointsPerStreak; point++)
                _points[first + point] = start;

            _age[streak] = 0f;
            _life[streak] = Random.Range(MinLife, MaxLife);
            _strength[streak] = strength;
        }

        private void Advance(int streak, float seconds, in AtmosRenderContext.Snapshot snapshot)
        {
            int first = streak * PointsPerStreak;
            Vector3 head = _points[first];
            Vector2 flow = AtmosCellVisualEncoding.SampleFlow(snapshot.FlowData, snapshot.AtlasBounds, head.x, head.z);
            float strength = flow.magnitude;
            _strength[streak] = strength;

            // A streak that drifts into still air or a wall dies quickly instead of hanging there.
            _age[streak] += strength < MinFlow * 0.5f ? seconds * 4f : seconds;

            head += new Vector3(flow.x, 0f, flow.y) * (MetresPerSecondAtFullFlow * seconds);
            _points[first] = head;

            float follow = 1f - Mathf.Exp(-TrailFollowRate * seconds);
            for (int point = 1; point < PointsPerStreak; point++)
                _points[first + point] = Vector3.Lerp(_points[first + point], _points[first + point - 1], follow);
        }

        private void WriteRibbon(int streak, Vector3 cameraPosition)
        {
            int first = streak * PointsPerStreak;
            float lifeFade = Mathf.Sin(Mathf.Clamp01(_age[streak] / _life[streak]) * Mathf.PI);
            float alpha = lifeFade * Mathf.Clamp01(_strength[streak] * 1.5f) * MaxAlpha;

            for (int point = 0; point < PointsPerStreak; point++)
            {
                Vector3 position = _points[first + point];
                int previous = first + Mathf.Max(point - 1, 0);
                int next = Mathf.Min(point + 1, PointsPerStreak - 1) + first;
                Vector3 tangent = _points[previous] - _points[next];
                Vector3 side = Vector3.Cross(tangent, position - cameraPosition);
                float length = side.magnitude;
                side = length > 1e-6f ? side / length : Vector3.zero;

                float along = point / (float)(PointsPerStreak - 1);
                Vector3 width = side * (HalfWidth * Mathf.Sin(along * Mathf.PI));
                Color32 colour = StreakColour;
                colour.a = (byte)(alpha * Mathf.Sin(along * Mathf.PI) * 255f);

                int vertex = (first + point) * 2;
                _vertices[vertex] = position + width;
                _vertices[vertex + 1] = position - width;
                _colours[vertex] = colour;
                _colours[vertex + 1] = colour;
            }
        }

        private void HideStreak(int streak)
        {
            int firstVertex = streak * PointsPerStreak * 2;
            for (int vertex = firstVertex; vertex < firstVertex + PointsPerStreak * 2; vertex++)
            {
                _vertices[vertex] = Vector3.zero;
                _colours[vertex] = default;
            }
        }

        private static int[] BuildTriangles()
        {
            var triangles = new int[StreakCount * (PointsPerStreak - 1) * 6];
            int index = 0;
            for (int streak = 0; streak < StreakCount; streak++)
            {
                int first = streak * PointsPerStreak * 2;
                for (int point = 0; point < PointsPerStreak - 1; point++)
                {
                    int vertex = first + point * 2;
                    triangles[index++] = vertex;
                    triangles[index++] = vertex + 1;
                    triangles[index++] = vertex + 2;
                    triangles[index++] = vertex + 1;
                    triangles[index++] = vertex + 3;
                    triangles[index++] = vertex + 2;
                }
            }

            return triangles;
        }
    }
}
