using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.StructuralDamage
{
    internal static class SteelWallDamageVfx
    {
        private static readonly int CrackedId = Shader.PropertyToID("_Cracked");
        private static readonly int ParticleId = Shader.PropertyToID("_Particle");
        private static Material _dent;
        private static Material _crack;
        private static Material _particle;

        public static bool IsSteelWall(PlacedTileObject wall)
        {
            return wall.tileObjectSO != null && wall.GenericType == TileObjectGenericType.Wall;
        }

        public static bool SupportsOverlay(PlacedTileObject placed)
        {
            return IsSteelWall(placed) || StructuralIntegrityRules.IsSupportedTable(placed.tileObjectSO)
                || StructuralIntegrityRules.IsMetalFurniture(placed.tileObjectSO)
                || (placed.tileObjectSO != null && placed.GenericType == TileObjectGenericType.Door);
        }

        public static bool IsDamageSurface(MeshRenderer renderer, PlacedTileObject placed)
        {
            if (SS3D.Systems.Interactions.InteractionOutlineView.IsOutlineRenderer(renderer)
                || renderer.name == "Steel wall damage overlay")
                return false;

            // Never mark stored items or nested independent tile objects.
            if (renderer.GetComponentInParent<SS3D.Systems.Inventory.Items.Item>() != null
                || renderer.GetComponentInParent<PlacedTileObject>() != placed)
                return false;

            if (StructuralIntegrityRules.IsMetalFurniture(placed.tileObjectSO))
            {
                if (placed.TryGetComponent(out SS3D.Systems.Furniture.Locker locker))
                    return renderer.name == "Body" || renderer.name == "Door";
                return renderer.transform == placed.transform || renderer.name == "Body"
                    || renderer.name == "Paint" || renderer.name == "GibberBase"
                    || renderer.name == placed.name.Replace("(Clone)", "");
            }

            // Airlock control panels and the generated floor must retain their own presentation.
            return placed.GenericType != TileObjectGenericType.Door
                || renderer.name == "LeftDoor" || renderer.name == "RightDoor"
                || renderer.name == "Left Airlock" || renderer.name == "Right Airlock";
        }

        public static void ConfigureSurface(MeshRenderer overlay, Mesh mesh, PlacedTileObject placed)
        {
            if (IsSteelWall(placed))
                return;

            var block = new MaterialPropertyBlock();
            block.SetFloat("_SurfaceMode", StructuralIntegrityRules.IsSupportedTable(placed.tileObjectSO) ? 1f
                : StructuralIntegrityRules.IsMetalFurniture(placed.tileObjectSO)
                    && !placed.TryGetComponent(out SS3D.Systems.Furniture.Locker _) ? 3f : 2f);
            block.SetFloat("_Glass", StructuralIntegrityRules.IsGlassTable(placed.tileObjectSO) ? 1f : 0f);
            block.SetFloat("_Wood", StructuralIntegrityRules.IsWoodTable(placed.tileObjectSO) ? 1f : 0f);
            block.SetVector("_SurfaceCenter", mesh.bounds.center);
            block.SetVector("_SurfaceExtents", mesh.bounds.extents);
            // Stable per tile, shared across stages and clients. Never animate the mark's position.
            float variation = Mathf.Repeat(placed.WorldOrigin.x * 0.618f + placed.WorldOrigin.y * 0.414f, 1f);
            block.SetFloat("_Variation", variation);
            overlay.SetPropertyBlock(block);
        }

        public static MeshRenderer CreateOverlay(MeshRenderer surface)
        {
            if (!surface.TryGetComponent(out MeshFilter source) || source.sharedMesh == null)
                return null;

            GameObject overlay = new("Steel wall damage overlay");
            overlay.transform.SetParent(surface.transform, false);
            overlay.layer = surface.gameObject.layer;
            overlay.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static void SetStage(MeshRenderer overlay, Mesh mesh, bool cracked)
        {
            if (!EnsureMaterials())
                return;

            overlay.GetComponent<MeshFilter>().sharedMesh = mesh;
            int slots = mesh.subMeshCount;
            Material[] materials = new Material[slots];
            for (int i = 0; i < slots; i++)
                materials[i] = cracked ? _crack : _dent;

            overlay.sharedMaterials = materials;
        }

        public static void PlayImpact(Vector3 point, Vector3 normal, byte material = 0)
        {
            if (!EnsureMaterials())
                return;

            Vector3 origin = point + normal * 0.025f;
            Quaternion rotation = Quaternion.LookRotation(normal);
            if (material == 2)
            {
                Emit(origin, rotation, "Glass chips", 10, 1.8f, 0.27f, 0.035f, 0.8f,
                    new Color(0.7f, 0.88f, 0.9f, 0.8f), ParticleSystemRenderMode.Stretch);
                return;
            }

            if (material == 1)
            {
                Emit(origin, rotation, "Wood chips", 8, 1.5f, 0.3f, 0.055f, 0.8f,
                    new Color(0.65f, 0.43f, 0.22f, 1f), ParticleSystemRenderMode.Stretch);
                Emit(origin, rotation, "Wood dust", 7, 0.45f, 0.3f, 0.1f, 0f,
                    new Color(0.52f, 0.36f, 0.2f, 0.45f), ParticleSystemRenderMode.Billboard);
                return;
            }

            Emit(origin, rotation, "Metal flecks", 10, 2.6f, 0.17f, 0.042f, 0.65f,
                new Color(0.95f, 0.79f, 0.52f, 1f), ParticleSystemRenderMode.Stretch);
            Emit(origin, rotation, "Dust", 9, 0.63f, 0.25f, 0.13f, 0f,
                new Color(0.54f, 0.49f, 0.43f, 0.5f), ParticleSystemRenderMode.Billboard);
        }

        /// <summary>A short shower of bright, falling sparks: a machine shorting out as it breaks.</summary>
        public static void PlaySparks(Vector3 point, Vector3 normal)
        {
            if (!EnsureMaterials())
                return;

            Quaternion rotation = Quaternion.LookRotation(normal);
            Vector3 origin = point + normal * 0.03f;
            Emit(origin, rotation, "Sparks", 28, 3.4f, 0.45f, 0.03f, 1.6f,
                new Color(1f, 0.86f, 0.45f, 1f), ParticleSystemRenderMode.Stretch);
            Emit(origin, rotation, "Sparks smoke", 6, 0.35f, 0.9f, 0.16f, -0.05f,
                new Color(0.35f, 0.35f, 0.38f, 0.4f), ParticleSystemRenderMode.Billboard);
        }

        private static bool EnsureMaterials()
        {
            if (_dent != null)
                return true;

            Shader shader = Resources.Load<Shader>("StructuralDamage/SteelWallDamage");
            if (shader == null || !shader.isSupported)
                return false;

            _dent = new Material(shader) { name = "Steel dent", hideFlags = HideFlags.HideAndDontSave };
            _crack = new Material(shader) { name = "Steel tear", hideFlags = HideFlags.HideAndDontSave };
            _crack.SetFloat(CrackedId, 1f);
            _particle = new Material(shader) { name = "Steel impact", hideFlags = HideFlags.HideAndDontSave };
            _particle.SetFloat(ParticleId, 1f);
            return true;
        }

        private static void Emit(Vector3 origin, Quaternion rotation, string name, int count, float speed, float lifetime,
            float size, float gravity, Color color, ParticleSystemRenderMode renderMode)
        {
            GameObject emitter = new(name);
            emitter.SetActive(false);
            emitter.transform.SetPositionAndRotation(origin, rotation);
            ParticleSystem particles = emitter.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.duration = lifetime;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 52f;
            shape.radius = 0.028f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particle;
            renderer.renderMode = renderMode;
            renderer.lengthScale = 1.7f;
            renderer.velocityScale = 0.025f;
            emitter.SetActive(true);
            particles.Play();
        }
    }
}
