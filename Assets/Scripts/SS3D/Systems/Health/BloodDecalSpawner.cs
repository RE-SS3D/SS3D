using Coimbra;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Rendering.URP;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Spawns persistent URP blood decals on world surfaces beneath bleeding characters.
    /// </summary>
    public static class BloodDecalSpawner
    {
        private const int MaxActiveDecals = 256;
        private const float RaycastHeight = 1.6f;
        private const float RaycastDistance = 4f;

        private static GameObject _floorDecalPrefab;
        private static Material _bloodDecalMaterialTemplate;
        private static int _surfaceMask = -1;
        private static readonly List<GameObject> ActiveDecals = new();

        public static Material BloodDecalMaterial
        {
            get
            {
                EnsureAssetsLoaded();
                return _bloodDecalMaterialTemplate;
            }
        }

        /// <summary>False without a graphics device (dedicated server): its build strips shaders.</summary>
        public static bool IsSupported
        {
            get
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                {
                    return false;
                }

                EnsureAssetsLoaded();
                return _bloodDecalMaterialTemplate != null;
            }
        }

        private static int SurfaceMask
        {
            get
            {
                if (_surfaceMask < 0)
                {
                    // Floors/tiles live on Default; never stamp onto the bleeding body itself.
                    _surfaceMask = ~LayerMask.GetMask("Characters", "BodyParts", "Items", "UI", "TransparentFX", "Ignore Raycast");
                }

                return _surfaceMask;
            }
        }

        public static void SpawnAtAnchor(Transform anchor, float intensityScale)
        {
            if (anchor == null || !IsSupported)
            {
                return;
            }

            Vector3 lateralOffset = new(
                Random.Range(-0.15f, 0.15f),
                0f,
                Random.Range(-0.15f, 0.15f));

            // Start above the character so we clear body colliders, then drop to the floor.
            Vector3 origin = new Vector3(anchor.position.x, anchor.position.y, anchor.position.z)
                + Vector3.up * RaycastHeight
                + lateralOffset;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RaycastDistance, SurfaceMask, QueryTriggerInteraction.Ignore))
            {
                // Fallback: try from slightly above the anchor in case the character is prone / below nominal height.
                origin = anchor.position + Vector3.up * 0.35f + lateralOffset;
                if (!Physics.Raycast(origin, Vector3.down, out hit, RaycastDistance, SurfaceMask, QueryTriggerInteraction.Ignore))
                {
                    return;
                }
            }

            SpawnOnSurface(hit.point, hit.normal, intensityScale);
        }

        private static void SpawnOnSurface(Vector3 point, Vector3 normal, float intensityScale)
        {
            GameObject instance = CreateDecalInstance();
            if (instance == null)
            {
                return;
            }

            // DecalProjector projects along local +Z into the surface. LookRotation(-normal)
            // is degenerate on flat floors (forward anti-parallel to default up) and produces a
            // tilted box — splat appears cut off and paints through standing characters.
            instance.transform.SetPositionAndRotation(
                point + normal * 0.02f,
                DecalRotationOntoSurface(normal, Random.Range(0f, 360f)));

            if (instance.TryGetComponent(out DecalProjector projector))
            {
                // Mask first, then a property that calls OnValidate — renderingLayerMask's
                // setter does not refresh DecalEntityManager by itself.
                projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
                projector.material = CreateRandomDecalMaterial();
                float size = Random.Range(0.3f, 0.55f) * intensityScale;
                // Shallow volume: deep boxes paint character feet inside the projector AABB.
                projector.size = new Vector3(size, size, 0.15f);
                projector.pivot = new Vector3(0f, 0f, 0.08f);
                projector.fadeFactor = Random.Range(0.85f, 1f);
                projector.uvScale = new Vector2(Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f));
                projector.drawDistance = 40f;
            }

            ActiveDecals.Add(instance);
            TrimOldDecals();
        }

        /// <summary>
        /// Align projector local +Z into the surface, then spin the splat in the surface plane.
        /// </summary>
        /// <remarks>
        /// URP DecalProjector projects along local +Z (after its internal -90°X remap).
        /// <see cref="Quaternion.LookRotation(Vector3, Vector3)"/>'s second argument is
        /// <b>upwards</b>, not a surface tangent — passing <c>Cross(into, up)</c> rolled wall
        /// projectors 90° so the volume grazed the face and stamped nothing. Floors need a
        /// non-up hint because into-surface is parallel to world up.
        /// </remarks>
        public static Quaternion RotationOntoSurface(Vector3 surfaceNormal, float spinDegrees)
        {
            Vector3 intoSurface = -surfaceNormal.normalized;
            // Projecting vertically: world up is parallel to into-surface — use forward as the up hint.
            // Projecting onto walls: world up keeps the stamp upright on the face.
            Vector3 upwards = Mathf.Abs(Vector3.Dot(intoSurface, Vector3.up)) > 0.99f
                ? Vector3.forward
                : Vector3.up;
            Quaternion align = Quaternion.LookRotation(intoSurface, upwards);
            return align * Quaternion.Euler(0f, 0f, spinDegrees);
        }

        private static Quaternion DecalRotationOntoSurface(Vector3 normal, float spinDegrees)
        {
            return RotationOntoSurface(normal, spinDegrees);
        }

        private static GameObject CreateDecalInstance()
        {
            EnsureAssetsLoaded();

            if (_floorDecalPrefab != null)
            {
                return Object.Instantiate(_floorDecalPrefab);
            }

            var go = new GameObject("BloodFloorDecal");
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = _bloodDecalMaterialTemplate;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.size = new Vector3(0.45f, 0.45f, 0.15f);
            projector.pivot = new Vector3(0f, 0f, 0.08f);
            projector.drawDistance = 40f;
            projector.startAngleFade = 180f;
            projector.endAngleFade = 180f;
            projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
            return go;
        }

        private static Material CreateRandomDecalMaterial()
        {
            BleedingVfxCatalog catalog = BleedingVfxCatalog.Instance;
            if (catalog != null)
            {
                Material instance = catalog.CreateDecalMaterial();
                if (instance != null)
                {
                    return instance;
                }
            }

            return _bloodDecalMaterialTemplate != null ? new Material(_bloodDecalMaterialTemplate) : null;
        }

        public static Material CreateBodyDecalMaterial()
        {
            EnsureAssetsLoaded();
            return CreateRandomDecalMaterial();
        }

        private static void EnsureAssetsLoaded()
        {
            if (_bloodDecalMaterialTemplate != null)
            {
                return;
            }

            BleedingVfxCatalog catalog = BleedingVfxCatalog.Instance;
            if (catalog != null)
            {
                _bloodDecalMaterialTemplate = catalog.BloodDecalMaterial;
                _floorDecalPrefab = catalog.FloorDecalPrefab;
            }

            if (_floorDecalPrefab == null)
            {
                _floorDecalPrefab = Assets.Get<GameObject>(AssetDatabases.ParticlesEffects, ParticlesEffects.BloodFloorDecal);
            }

            if (_bloodDecalMaterialTemplate == null
                && _floorDecalPrefab != null
                && _floorDecalPrefab.TryGetComponent(out DecalProjector projector))
            {
                _bloodDecalMaterialTemplate = projector.material;
            }
        }

        /// <summary>Releases this client's surface marks at the round boundary.</summary>
        public static void Clear()
        {
            foreach (GameObject decal in ActiveDecals)
            {
                DestroyDecal(decal);
            }

            ActiveDecals.Clear();
        }

        private static void TrimOldDecals()
        {
            while (ActiveDecals.Count > MaxActiveDecals)
            {
                GameObject oldest = ActiveDecals[0];
                ActiveDecals.RemoveAt(0);
                DestroyDecal(oldest);
            }
        }

        private static void DestroyDecal(GameObject decal)
        {
            if (decal == null)
            {
                return;
            }

            if (decal.TryGetComponent(out DecalProjector projector)
                && projector.material != null
                && projector.material != _bloodDecalMaterialTemplate)
            {
                Object.Destroy(projector.material);
            }

            decal.Dispose(true);
        }
    }
}
