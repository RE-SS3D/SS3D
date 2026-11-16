using Coimbra;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Ephemeral floor scorch marks at blast epicenters (URP DecalProjector, world receivers only).
    /// </summary>
    public static class BlastScorchSpawner
    {
        private const int MaxActiveDecals = 64;
        private const float RaycastHeight = 1.5f;
        private const float RaycastDistance = 4f;

        private static readonly List<GameObject> ActiveDecals = new();
        private static int _surfaceMask = -1;

        private static int SurfaceMask
        {
            get
            {
                if (_surfaceMask < 0)
                {
                    _surfaceMask = ~LayerMask.GetMask(
                        "Characters", "BodyParts", "Items", "UI", "TransparentFX", "Ignore Raycast", StationBounds.CeilingLayerName);
                }

                return _surfaceMask;
            }
        }

        public static void SpawnAt(Vector3 epicenter, float yieldScale, BlastVfxCatalog catalog)
        {
            if (catalog == null)
                return;

            Vector3 origin = epicenter + Vector3.up * RaycastHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RaycastDistance, SurfaceMask, QueryTriggerInteraction.Ignore))
            {
                origin = epicenter + Vector3.up * 0.4f;
                if (!Physics.Raycast(origin, Vector3.down, out hit, RaycastDistance, SurfaceMask, QueryTriggerInteraction.Ignore))
                    return;
            }

            GameObject instance = CreateDecalInstance(catalog);
            if (instance == null)
                return;

            instance.transform.SetPositionAndRotation(
                hit.point + hit.normal * 0.02f,
                BloodDecalSpawner.RotationOntoSurface(hit.normal, Random.Range(0f, 360f)));

            if (instance.TryGetComponent(out DecalProjector projector))
            {
                projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
                if (catalog.ScorchDecalMaterial != null)
                    projector.material = new Material(catalog.ScorchDecalMaterial);

                float size = Mathf.Lerp(0.7f, 1.8f, Mathf.InverseLerp(0.35f, 2.5f, yieldScale));
                projector.size = new Vector3(size, size, 0.18f);
                projector.pivot = new Vector3(0f, 0f, 0.09f);
                projector.fadeFactor = Random.Range(0.75f, 0.95f);
                projector.drawDistance = 40f;
            }

            ActiveDecals.Add(instance);
            TrimOldDecals(catalog);
        }

        private static GameObject CreateDecalInstance(BlastVfxCatalog catalog)
        {
            if (catalog.ScorchFloorPrefab != null)
                return Object.Instantiate(catalog.ScorchFloorPrefab);

            if (catalog.ScorchDecalMaterial == null)
                return null;

            var go = new GameObject("BlastScorchDecal");
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = catalog.ScorchDecalMaterial;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.size = new Vector3(1f, 1f, 0.18f);
            projector.pivot = new Vector3(0f, 0f, 0.09f);
            projector.drawDistance = 40f;
            projector.startAngleFade = 180f;
            projector.endAngleFade = 180f;
            projector.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
            return go;
        }

        private static void TrimOldDecals(BlastVfxCatalog catalog)
        {
            while (ActiveDecals.Count > MaxActiveDecals)
            {
                GameObject oldest = ActiveDecals[0];
                ActiveDecals.RemoveAt(0);
                if (oldest == null)
                    continue;

                if (oldest.TryGetComponent(out DecalProjector projector)
                    && projector.material != null
                    && catalog != null
                    && projector.material != catalog.ScorchDecalMaterial)
                {
                    Object.Destroy(projector.material);
                }

                oldest.Dispose(true);
            }
        }
    }
}
