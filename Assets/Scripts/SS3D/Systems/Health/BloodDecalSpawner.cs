using Coimbra;
using SS3D.Data;
using SS3D.Data.Generated;
using System.Collections.Generic;
using UnityEngine;
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

        public static bool IsSupported
        {
            get
            {
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

            // DecalProjector projects along local +Z; point that into the surface.
            instance.transform.SetPositionAndRotation(
                point + normal * 0.02f,
                Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            if (instance.TryGetComponent(out DecalProjector projector))
            {
                projector.material = CreateRandomDecalMaterial();
                float size = Random.Range(0.3f, 0.55f) * intensityScale;
                projector.size = new Vector3(size, size, Mathf.Max(0.5f, size * 1.2f));
                projector.pivot = new Vector3(0f, 0f, 0.25f);
                projector.fadeFactor = Random.Range(0.85f, 1f);
                projector.uvScale = new Vector2(Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f));
                projector.drawDistance = 40f;
            }

            ActiveDecals.Add(instance);
            TrimOldDecals();
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
            projector.size = new Vector3(0.45f, 0.45f, 0.8f);
            projector.pivot = new Vector3(0f, 0f, 0.25f);
            projector.drawDistance = 40f;
            projector.startAngleFade = 180f;
            projector.endAngleFade = 180f;
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

        private static void TrimOldDecals()
        {
            while (ActiveDecals.Count > MaxActiveDecals)
            {
                GameObject oldest = ActiveDecals[0];
                ActiveDecals.RemoveAt(0);
                if (oldest == null)
                {
                    continue;
                }

                if (oldest.TryGetComponent(out DecalProjector projector)
                    && projector.material != null
                    && projector.material != _bloodDecalMaterialTemplate)
                {
                    Object.Destroy(projector.material);
                }

                oldest.Dispose(true);
            }
        }
    }
}
