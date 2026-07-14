using Coimbra;
using SS3D.Data;
using SS3D.Data.Generated;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Per-zone bleeding particle VFX driven by synced health snapshots.
    /// </summary>
    public class WoundVfx : MonoBehaviour
    {
        private readonly Dictionary<BodyZone, GameObject> _activeParticles = new();
        private readonly Dictionary<BodyZone, Transform> _anchors = new();
        private GameObject _particlePrefab;
        private bool _anchorsBuilt;

        public void ApplySnapshot(HealthSnapshot snapshot)
        {
            EnsureAnchors();

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                BodyZone zone = (BodyZone)i;
                SetZoneBleeding(zone, snapshot.IsZoneBleeding(zone));
            }
        }

        private void EnsureAnchors()
        {
            if (_anchorsBuilt)
            {
                return;
            }

            AnatomyNode[] anatomyNodes = GetComponentsInChildren<AnatomyNode>(true);
            for (int i = 0; i < anatomyNodes.Length; i++)
            {
                AnatomyNode node = anatomyNodes[i];
                _anchors.TryAdd(node.PrimaryZone, node.transform);
            }

            ZoneTargetCollider[] zoneColliders = GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < zoneColliders.Length; i++)
            {
                ZoneTargetCollider zoneCollider = zoneColliders[i];
                _anchors.TryAdd(zoneCollider.Zone, zoneCollider.transform);
            }

            _anchorsBuilt = true;
        }

        private void SetZoneBleeding(BodyZone zone, bool bleeding)
        {
            if (!bleeding)
            {
                if (_activeParticles.TryGetValue(zone, out GameObject existing))
                {
                    existing.SetActive(false);
                }

                return;
            }

            if (!_anchors.TryGetValue(zone, out Transform anchor) || anchor == null)
            {
                return;
            }

            if (!_activeParticles.TryGetValue(zone, out GameObject particle) || particle == null)
            {
                GameObject prefab = GetParticlePrefab();
                if (prefab == null)
                {
                    return;
                }

                particle = Instantiate(prefab, anchor);
                _activeParticles[zone] = particle;
            }

            particle.transform.SetParent(anchor, false);
            particle.transform.localPosition = Vector3.zero;
            particle.SetActive(true);
        }

        private GameObject GetParticlePrefab()
        {
            if (_particlePrefab == null)
            {
                _particlePrefab = Assets.Get<GameObject>(AssetDatabases.ParticlesEffects, ParticlesEffects.BleedingParticle);
            }

            return _particlePrefab;
        }

        private void OnDestroy()
        {
            foreach (GameObject particle in _activeParticles.Values)
            {
                if (particle != null)
                {
                    particle.Dispose(true);
                }
            }

            _activeParticles.Clear();
        }
    }
}
