using Coimbra;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Deterministic folded panels and table remains. Only recent destruction animates.</summary>
    public sealed class SteelWreckageView : MonoBehaviour
    {
        private sealed class Piece
        {
            public Transform Transform;
            public Vector3 Start, End;
            public Quaternion StartRotation, EndRotation;
            public float FallTime;
            public Vector3 Spin;
        }
        private sealed class Wreck
        {
            public SteelWreckageRecord Record;
            public GameObject Root;
            public readonly List<Piece> Pieces = new();
            public float Age = 2f;
        }
        private readonly Dictionary<(Tile.TileCoord, Tile.TileLayer), Wreck> _wrecks = new();
        private readonly HashSet<(Tile.TileCoord, Tile.TileLayer)> _live = new();
        private readonly List<(Tile.TileCoord, Tile.TileLayer)> _remove = new();
        private Material _steel;
        private Material _dust;
        private Mesh _panel;

        public void Reconcile(IEnumerable<SteelWreckageRecord> records)
        {
            _live.Clear();
            foreach (var record in records)
            {
                var key = (record.Coord, record.Layer);
                _live.Add(key);
                if (!_wrecks.ContainsKey(key)) _wrecks.Add(key, Build(record));
            }
            _remove.Clear();
            foreach (var pair in _wrecks)
                if (!_live.Contains(pair.Key)) _remove.Add(pair.Key);
            foreach (var key in _remove)
            {
                _wrecks[key].Root.Dispose(true);
                _wrecks.Remove(key);
            }
        }

        public void BreakApart(SteelWreckageRecord record)
        {
            var key = (record.Coord, record.Layer);
            if (!_wrecks.TryGetValue(key, out Wreck wreck))
                _wrecks.Add(key, wreck = Build(record));
            wreck.Age = 0f;
            foreach (Piece piece in wreck.Pieces)
            {
                piece.Transform.localPosition = piece.Start;
                piece.Transform.localRotation = piece.StartRotation;
            }
            EmitDust(wreck, false);
            EmitDust(wreck, true);
            SteelWallDamageVfx.PlayImpact(record.Position + Vector3.up * Mathf.Min(record.Size.y * 0.5f, 1f), Vector3.up, 0);
        }

        private void EmitDust(Wreck wreck, bool groundImpact)
        {
            if (_dust == null)
            {
                Shader shader = Resources.Load<Shader>("StructuralDamage/CollapseDust");
                if (shader == null || !shader.isSupported) return;
                _dust = new Material(shader) { name = "Collapse dust" };
            }
            bool table = wreck.Record.Kind == 2;
            GameObject emitter = new(groundImpact ? "Ground impact dust" : "Breakup dust cloud");
            emitter.SetActive(false);
            emitter.transform.SetParent(wreck.Root.transform, false);
            emitter.transform.localPosition = Vector3.up * (groundImpact ? 0.12f : Mathf.Min(wreck.Record.Size.y * 0.45f, 0.9f));
            emitter.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem particles = emitter.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.15f;
            main.startDelay = groundImpact ? 0.32f : 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, groundImpact ? 1.35f : 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(groundImpact ? 0.35f : 0.15f, groundImpact ? 1.1f : 0.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(table ? 0.25f : 0.4f, table ? 0.65f : 1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor = new Color(0.56f, 0.55f, 0.51f, groundImpact ? 0.32f : 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;
            main.stopAction = ParticleSystemStopAction.Destroy;
            particles.useAutoRandomSeed = false;
            particles.randomSeed = unchecked((uint)wreck.Record.Seed) + (groundImpact ? 2u : 1u);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(table ? 12 : 22)) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = groundImpact ? 78f : 65f;
            shape.radius = Mathf.Clamp(Mathf.Max(wreck.Record.Size.x, wreck.Record.Size.z) * 0.3f, 0.2f, 0.65f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.12f);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.9f));
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f) }, new[] {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _dust;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            emitter.SetActive(true);
            particles.Play();
        }

        private Wreck Build(SteelWreckageRecord record)
        {
            EnsureAssets();
            var wreck = new Wreck { Record = record, Root = new GameObject(record.Kind == 2 ? "Collapsed steel table" : "Steel wall rubble") };
            wreck.Root.transform.SetParent(transform, false);
            wreck.Root.transform.position = record.Position;
            // Size is a world-space AABB, so keep footprint axes in world space as well.
            var random = new System.Random(record.Seed);
            float Next(float min, float max) => min + (max - min) * (float)random.NextDouble();
            float width = Mathf.Clamp(record.Size.x, 0.5f, 1.9f);
            float depth = Mathf.Clamp(record.Size.z, 0.45f, 1.9f);
            bool table = record.Kind == 2;
            int count = table ? 7 : 11;
            for (int i = 0; i < count; i++)
            {
                bool leg = table && i >= 3;
                var go = new GameObject(leg ? "Folded table leg" : "Torn folded steel panel");
                go.transform.SetParent(wreck.Root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _panel;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _steel;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                float sx = leg ? 0.055f : width * Next(0.25f, 0.52f);
                float sz = leg ? Mathf.Clamp(record.Size.y * 0.8f, 0.35f, 0.7f) : depth * Next(0.35f, 0.7f);
                go.transform.localScale = new Vector3(sx, leg ? 0.07f : Next(0.13f, 0.22f), sz);
                var endRotation = Quaternion.Euler(Next(-32f, 32f), Next(0f, 360f), Next(-28f, 28f));
                // Rest the actual rotated geometry on the floor rather than floating its pivot above it.
                float bottom = float.PositiveInfinity;
                foreach (Vector3 vertex in _panel.vertices)
                    bottom = Mathf.Min(bottom, (endRotation * Vector3.Scale(vertex, go.transform.localScale)).y);
                float pile = table ? (i < 3 ? i * 0.045f : 0f) : (i % 3) * 0.045f;
                var end = new Vector3(Next(-width * 0.33f, width * 0.33f), -bottom + pile, Next(-depth * 0.34f, depth * 0.34f));
                var piece = new Piece
                {
                    Transform = go.transform, End = end, EndRotation = endRotation,
                    Start = new Vector3(end.x * 0.4f, table ? record.Size.y * (leg ? 0.45f : 0.93f) : Next(0.3f, Mathf.Max(0.4f, record.Size.y)), end.z * 0.3f),
                    StartRotation = table ? Quaternion.Euler(leg ? 90f : 0f, record.Yaw, 0f) : Quaternion.Euler(90f, record.Yaw, Next(-25f, 25f)),
                    Spin = new Vector3(Next(-170f, 170f), Next(-120f, 120f), Next(-170f, 170f))
                };
                piece.Start.y = Mathf.Max(piece.Start.y, end.y + 0.12f);
                piece.FallTime = Mathf.Sqrt(2f * (piece.Start.y - end.y) / 18f);
                go.transform.localPosition = end;
                go.transform.localRotation = endRotation;
                wreck.Pieces.Add(piece);
            }
            return wreck;
        }

        private void Update()
        {
            foreach (Wreck wreck in _wrecks.Values)
            {
                if (wreck.Age >= 1f) continue;
                wreck.Age += Time.deltaTime;
                foreach (Piece piece in wreck.Pieces)
                {
                    float t = Mathf.Clamp01(wreck.Age / piece.FallTime);
                    Vector3 position = Vector3.Lerp(piece.Start, piece.End, t);
                    // Constant downward acceleration: speed builds until impact, with no slow ease-out.
                    position.y = Mathf.Lerp(piece.Start.y, piece.End.y, t * t);
                    float afterImpact = wreck.Age - piece.FallTime;
                    if (afterImpact > 0f && afterImpact < 0.16f)
                        position.y += Mathf.Sin(afterImpact / 0.16f * Mathf.PI) * 0.045f;
                    piece.Transform.localPosition = position;
                    piece.Transform.localRotation = Quaternion.Slerp(piece.StartRotation, piece.EndRotation, t)
                        * Quaternion.Euler(piece.Spin * Mathf.Sin(t * Mathf.PI));
                }
            }
        }

        private void EnsureAssets()
        {
            if (_steel == null)
                _steel = new Material(Resources.Load<Shader>("StructuralDamage/SteelWreckage")) { name = "Wreckage steel" };
            if (_panel != null) return;
            // Jagged outline, raised fold through the centre, and dark visible thickness.
            Vector3[] top = {
                new(-0.5f, 0, -0.5f), new(0.05f, 1.5f, -0.43f), new(0.48f, 0, -0.5f),
                new(-0.43f, 0, 0.5f), new(-0.04f, 1.1f, 0.42f), new(0.5f, 0, 0.36f) };
            var vertices = new Vector3[12];
            for (int i = 0; i < 6; i++) { vertices[i] = top[i]; vertices[i + 6] = top[i] - Vector3.up * 0.65f; }
            var triangles = new List<int> { 0,3,1, 1,3,4, 1,4,2, 2,4,5, 6,7,9, 7,10,9, 7,8,10, 8,11,10 };
            int[] edge = { 0,1,2,5,4,3 };
            for (int i = 0; i < edge.Length; i++)
            {
                int a = edge[i], b = edge[(i + 1) % edge.Length];
                triangles.AddRange(new[] { a,b,a+6, b,b+6,a+6 });
            }
            _panel = new Mesh { name = "Folded jagged steel" };
            // Separate face vertices keep torn edges and folds crisp under station lights.
            var faceted = new Vector3[triangles.Count];
            var indices = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++) { faceted[i] = vertices[triangles[i]]; indices[i] = i; }
            _panel.vertices = faceted;
            _panel.SetTriangles(indices, 0);
            _panel.RecalculateNormals();
            _panel.RecalculateBounds();
        }

        private void OnDestroy()
        {
            foreach (Wreck wreck in _wrecks.Values) if (wreck.Root != null) wreck.Root.Dispose(true);
            if (_steel != null) Destroy(_steel);
            if (_dust != null) Destroy(_dust);
            if (_panel != null) Destroy(_panel);
        }
    }
}
