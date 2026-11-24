using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Which row of the debris atlas a burst draws from.</summary>
    public enum DebrisKind : byte
    {
        Metal = 0,
        Glass = 1,
        Wood = 2,
        Electronics = 3,
    }

    /// <summary>
    /// Flat cartoon fragments from the SS3D-Art particle sheet. They fly out, spin while moving, bounce off floors
    /// and walls, settle, then fade. Client presentation only, unbuffered, so late joiners never see old debris.
    /// </summary>
    public static class DebrisSpriteVfx
    {
        public const int Columns = 10;
        public const int Rows = 4;
        public const int CellSize = 64;
        public const string SourceFolder = "Assets/Art/Textures/World/VFX/Particles";

        /// <summary>
        /// First bolt/nut/gear/spring cell in the metal row. Cells before it are whole panels, which read as plain
        /// squares on a small hit, so contact hits use only the hardware and collapses use the full row.
        /// </summary>
        public const int MetalHardwareStart = 5;

        /// <summary>One knob for how large every fragment draws; call sites pass sizes relative to each other.</summary>
        public const float SizeScale = 1.75f;

        /// <summary>Share of the local wind speed a fragment is carried at: light scraps go with the air.</summary>
        public const float WindCarry = 1f;

        /// <summary>Wind faster than this (m/s, knockdown is 5) carries debris no further, so a breach doesn't fling it.</summary>
        public const float MaxCarriedWind = 4f;

        /// <summary>Below this pressure a fragment is in space and does not fall.</summary>
        public const float VacuumKpa = 5f;

        /// <summary>
        /// World drift for a fragment in published atmos flow (<see cref="AtmosCellVisualEncoding.SampleFlow"/>).
        /// Still air gives zero, so bursts away from a breach behave as before.
        /// </summary>
        public static Vector3 WindDrift(Vector2 flow)
        {
            float speed = Mathf.Min(AtmosCellVisualEncoding.DecodeWindSpeed(flow), MaxCarriedWind);
            if (speed < 0.05f)
                return Vector3.zero;

            Vector2 direction = flow.normalized;
            return new Vector3(direction.x, 0f, direction.y) * (speed * WindCarry);
        }

        /// <summary>
        /// Source sprites per row, in <see cref="DebrisKind"/> order. The editor recipe packs the atlas from this
        /// table, so a change here needs a recipe re-run.
        /// </summary>
        public static readonly string[][] Sprites =
        {
            new[]
            {
                "ParticleMechanicalPlate1", "ParticleMechanicalPlate2", "ParticleMechanicalPlate3",
                "ParticleMechanicalPlate4", "ParticleMechanicalPlate5", "ParticleMechanicalJunk1",
                "ParticleMechanicalJunk2", "ParticleMechanicalJunk3", "ParticleMechanicalJunk4",
                "ParticleMechanicalJunk5",
            },
            new[]
            {
                "ParticleGlassShard1", "ParticleGlassShard2", "ParticleGlassShard3", "ParticleGlassShard4",
                "ParticleGlassShard5", "ParticleGlassShard6", "ParticleGlassShard7",
            },
            new[] { "ParticleMaterialWood1", "ParticleMaterialWood2", "ParticleMaterialWood3" },
            new[]
            {
                "ParticleElectronicsPart1", "ParticleElectronicsPart2", "ParticleElectronicsPart3",
                "ParticleElectronicsPart4", "ParticleElectronicsPart5", "ParticleElectronicsPart6",
                "ParticleElectronicsWire1", "ParticleElectronicsWire2", "ParticleElectronicsWire3",
                "ParticleElectronicsWire4",
            },
        };

        private static Material _material;

        public static DebrisKind ForMaterial(StructuralMaterial material)
        {
            return material switch
            {
                StructuralMaterial.Glass => DebrisKind.Glass,
                StructuralMaterial.Wood => DebrisKind.Wood,
                _ => DebrisKind.Metal,
            };
        }

        /// <summary>A broken window, table or door: fragments burst out on every side.</summary>
        public static void PlayBreak(Vector3 point, StructuralMaterial material)
        {
            DebrisKind kind = ForMaterial(material);
            int count = kind == DebrisKind.Glass ? 18 : kind == DebrisKind.Wood ? 12 : 10;
            Burst(point, Vector3.up, kind, count, 2.6f, kind == DebrisKind.Glass ? 0.12f : 0.15f, 180f);
        }

        /// <summary>Fragment cap per debris kind for one blast; Unity drops further emits once it is full.</summary>
        public const int MaxBlastParticles = 1500;

        private static readonly Dictionary<(int Blast, DebrisKind Kind), ParticleSystem> BlastEmitters = new();
        private static readonly List<(int, DebrisKind)> DeadEmitters = new();

        /// <summary>
        /// Emits <paramref name="count"/> fragments along <paramref name="direction"/> inside a cone of
        /// <paramref name="spread"/> degrees; 180 or more is a full sphere. Returns null without a graphics device.
        /// Debris from a fresh blast (<see cref="BlastDebrisPush"/>) is thrown away from the epicenter instead, into
        /// one shared emitter per blast and kind: a big blast breaks hundreds of tiles, and an emitter each cost
        /// about 50 ms to create plus a wind pass per emitter every frame.
        /// </summary>
        public static ParticleSystem Burst(Vector3 origin, Vector3 direction, DebrisKind kind, int count, float speed,
            float size, float spread = 55f, float lifetime = 2.4f, int firstSprite = 0)
        {
            if (count <= 0 || !EnsureMaterial())
                return null;

            if (BlastDebrisPush.TryGetPush(origin, out Vector3 thrown, out float thrownSpeed, out int blast))
            {
                return EmitIntoBlast(blast, origin, thrown, kind, count, Mathf.Max(speed, thrownSpeed), size,
                    Mathf.Min(spread, BlastDebrisPush.Spread), Mathf.Max(lifetime, 2.4f));
            }

            ParticleSystem particles = CreateEmitter($"{kind} debris", kind, count, speed, firstSprite);
            GameObject emitter = particles.gameObject;
            Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            emitter.transform.SetPositionAndRotation(origin, rotation);

            var main = particles.main;
            main.duration = 0.05f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            size *= SizeScale;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = particles.shape;
            if (spread >= 180f)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.12f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spread;
                shape.radius = 0.04f;
            }

            emitter.SetActive(true);
            particles.Play();
            return particles;
        }

        private static ParticleSystem EmitIntoBlast(int blast, Vector3 origin, Vector3 direction, DebrisKind kind,
            int count, float speed, float size, float spread, float lifetime)
        {
            if (!BlastEmitters.TryGetValue((blast, kind), out ParticleSystem particles) || particles == null)
            {
                PruneBlastEmitters();
                particles = CreateEmitter($"{kind} blast debris", kind, MaxBlastParticles, BlastDebrisPush.MaxSpeed, 0);
                var main = particles.main;
                // Only breaks inside the push window join it; it destroys itself once the last fragment fades.
                main.duration = BlastDebrisPush.Window + 0.1f;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.enabled = false;
                particles.gameObject.SetActive(true);
                particles.Play();
                BlastEmitters[(blast, kind)] = particles;
            }

            Quaternion rotation = Quaternion.LookRotation(direction);
            float cosSpread = Mathf.Cos(Mathf.Clamp(spread, 0f, 180f) * Mathf.Deg2Rad);
            size *= SizeScale;
            for (int i = 0; i < count; i++)
            {
                // Uniform over the cone's spherical cap.
                float z = Random.Range(cosSpread, 1f);
                float phi = Random.Range(0f, 2f * Mathf.PI);
                float ring = Mathf.Sqrt(1f - z * z);
                Vector3 local = new(ring * Mathf.Cos(phi), ring * Mathf.Sin(phi), z);
                particles.Emit(new ParticleSystem.EmitParams
                {
                    position = origin + Random.insideUnitSphere * 0.12f,
                    velocity = rotation * local * (speed * Random.Range(0.4f, 1f)),
                    startSize = size * Random.Range(0.6f, 1f),
                    startLifetime = lifetime * Random.Range(0.7f, 1f),
                    rotation = Random.Range(-180f, 180f),
                    startColor = Color.white,
                }, 1);
            }

            return particles;
        }

        private static void PruneBlastEmitters()
        {
            DeadEmitters.Clear();
            foreach (var pair in BlastEmitters)
                if (pair.Value == null)
                    DeadEmitters.Add(pair.Key);
            foreach (var key in DeadEmitters)
                BlastEmitters.Remove(key);
        }

        /// <summary>
        /// An inactive emitter with everything a debris burst shares: sprites from <paramref name="kind"/>'s row
        /// from <paramref name="firstSprite"/>, spin, bounces, fade and wind. The caller sets the launch and plays it.
        /// </summary>
        private static ParticleSystem CreateEmitter(string name, DebrisKind kind, int maxParticles, float spinSpeed,
            int firstSprite)
        {
            GameObject emitter = new(name);
            emitter.SetActive(false);
            ParticleSystem particles = emitter.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startColor = Color.white;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.stopAction = ParticleSystemStopAction.Destroy;

            // One random sprite per particle from this kind's row, held for its whole life.
            var sheet = particles.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = Columns;
            sheet.numTilesY = Rows;
            sheet.animation = ParticleSystemAnimationType.SingleRow;
            sheet.rowMode = ParticleSystemAnimationRowMode.Custom;
            sheet.rowIndex = (int)kind;
            int spriteCount = Sprites[(int)kind].Length;
            firstSprite = Mathf.Clamp(firstSprite, 0, spriteCount - 1);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve((float)firstSprite / Columns,
                (spriteCount - 0.05f) / Columns);
            sheet.cycleCount = 1;

            // Spin scales with speed, so a fragment stops turning once it settles on the floor.
            var spin = particles.rotationBySpeed;
            spin.enabled = true;
            spin.range = new Vector2(0f, spinSpeed);
            spin.z = new ParticleSystem.MinMaxCurve(12f, AnimationCurve.Linear(0f, 0f, 1f, -1f),
                AnimationCurve.Linear(0f, 0f, 1f, 1f));

            var collision = particles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.collidesWith = LayerMask.GetMask("Default", "Walls");
            collision.enableDynamicColliders = false;
            collision.bounce = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            collision.dampen = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            collision.radiusScale = 0.4f;

            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.72f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.flip = new Vector3(0.5f, 0.5f, 0f);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Wind is followed per fragment every frame: a breach's own debris spawns before its air starts moving.
            emitter.AddComponent<DebrisWindCarrier>();
            return particles;
        }

        /// <summary>
        /// Wind drift and vacuum at a world position from the client's atmos snapshot, the one the wind streaks draw
        /// from. Still air with gravity when there is none, the tile is unknown, or the map editor suppresses atmos.
        /// </summary>
        public static void SampleAir(in AtmosRenderContext.Snapshot snapshot, Vector3 position, out Vector3 drift,
            out bool vacuum)
        {
            drift = WindDrift(AtmosCellVisualEncoding.SampleFlow(snapshot.FlowData, snapshot.AtlasBounds,
                position.x, position.z));
            AtmosCellVisualEncoding.CellRead read = AtmosCellVisualEncoding.ReadCell(snapshot.PressureData,
                snapshot.MaskData, snapshot.AtlasBounds, position.x, position.z, out float pressureKpa);
            vacuum = read == AtmosCellVisualEncoding.CellRead.Open && pressureKpa < VacuumKpa;
        }

        private static bool EnsureMaterial()
        {
            if (_material != null)
                return true;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return false;

            Texture2D atlas = DebrisVfxCatalog.Instance != null ? DebrisVfxCatalog.Instance.Atlas : null;
            Shader shader = Resources.Load<Shader>("StructuralDamage/DebrisSprite");
            if (atlas == null || shader == null || !shader.isSupported)
                return false;

            _material = new Material(shader) { name = "Debris sprites", mainTexture = atlas, hideFlags = HideFlags.HideAndDontSave };
            return true;
        }
    }
}
