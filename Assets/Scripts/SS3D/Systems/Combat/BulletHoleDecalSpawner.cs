using Coimbra;
using SS3D.Rendering.URP;
using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Spawns capped URP bullet-hole decals on non-living ranged impacts (walls/floors/props).
    /// </summary>
    public static class BulletHoleDecalSpawner
    {
        private const int MaxActiveDecals = 96;
        private const float MinSize = 0.12f;
        private const float MaxSize = 0.22f;
        private const float FloorVolumeDepth = 0.15f;
        // Walls are thin face skins on a full-cell box collider — keep enough depth that the
        // projector volume still overlaps the mesh if the hit lands slightly off the visual.
        private const float WallVolumeDepth = 0.45f;
        private const float SurfaceOutwardOffset = 0.02f;

        private static GameObject _floorDecalPrefab;
        private static Material _decalMaterialTemplate;
        private static readonly List<GameObject> ActiveDecals = new();

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
                return _decalMaterialTemplate != null;
            }
        }

        public static void Spawn(Vector3 point, Vector3 normal)
        {
            if (!IsSupported)
            {
                return;
            }

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.up;
            }
            else
            {
                normal.Normalize();
            }

            GameObject instance = CreateDecalInstance();
            if (instance == null)
            {
                return;
            }

            bool vertical = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.5f;
            float depth = vertical ? WallVolumeDepth : FloorVolumeDepth;

            // Sit just outside the face; pivot at depth/2 so the box straddles the surface
            // (same convention as floor blood — avoids half-clipped stamps).
            instance.transform.SetPositionAndRotation(
                point + normal * SurfaceOutwardOffset,
                BloodDecalSpawner.RotationOntoSurface(normal, Random.Range(0f, 360f)));

            if (instance.TryGetComponent(out DecalProjector projector))
            {
                // Mask first, then a property that calls OnValidate — renderingLayerMask's
                // setter does not refresh DecalEntityManager by itself.
                projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
                projector.material = CreateRandomDecalMaterial();
                float size = Random.Range(MinSize, MaxSize);
                projector.size = new Vector3(size, size, depth);
                projector.pivot = new Vector3(0f, 0f, depth * 0.5f);
                projector.startAngleFade = 180f;
                projector.endAngleFade = 180f;
                projector.fadeFactor = 1f;
                projector.uvScale = new Vector2(Random.Range(0.95f, 1.05f), Random.Range(0.95f, 1.05f));
                projector.drawDistance = 50f;
            }

            ActiveDecals.Add(instance);
            TrimOldDecals();
        }

        private static GameObject CreateDecalInstance()
        {
            EnsureAssetsLoaded();
            if (_floorDecalPrefab != null)
            {
                // Prefab is authored for floors (X=90). Spawn overwrites pose; strip that so
                // wall hits don't briefly register a floor-oriented entity.
                GameObject instance = Object.Instantiate(_floorDecalPrefab);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                return instance;
            }

            var go = new GameObject("BulletHoleDecal");
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = _decalMaterialTemplate;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.size = new Vector3(0.18f, 0.18f, FloorVolumeDepth);
            projector.pivot = new Vector3(0f, 0f, FloorVolumeDepth * 0.5f);
            projector.drawDistance = 50f;
            projector.startAngleFade = 180f;
            projector.endAngleFade = 180f;
            projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
            return go;
        }

        private static Material CreateRandomDecalMaterial()
        {
            BulletHoleVfxCatalog catalog = BulletHoleVfxCatalog.Instance;
            if (catalog != null)
            {
                Material instance = catalog.CreateDecalMaterial();
                if (instance != null)
                {
                    return instance;
                }
            }

            return _decalMaterialTemplate != null ? new Material(_decalMaterialTemplate) : null;
        }

        private static void EnsureAssetsLoaded()
        {
            if (_decalMaterialTemplate != null)
            {
                return;
            }

            BulletHoleVfxCatalog catalog = BulletHoleVfxCatalog.Instance;
            if (catalog != null)
            {
                _decalMaterialTemplate = catalog.DecalMaterial;
                _floorDecalPrefab = catalog.FloorDecalPrefab;
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
                && projector.material != _decalMaterialTemplate)
            {
                Object.Destroy(projector.material);
            }

            decal.Dispose(true);
        }
    }
}
