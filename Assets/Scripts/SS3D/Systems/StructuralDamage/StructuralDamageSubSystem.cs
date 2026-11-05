using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Entry point for structural integrity damage (walls, doors, windows) and blast BFS.
    /// Self-bootstraps — do not add to Boot.unity.
    /// </summary>
    public sealed class StructuralDamageSubSystem : SubSystem
    {
        private StructuralDamageService _service;
        private BlastResolutionService _blast;
        private ITileQueryService _boundQuery;
        private IConstructionService _boundConstruction;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out StructuralDamageSubSystem _))
                return;

            GameObject host = new(nameof(StructuralDamageSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<StructuralDamageSubSystem>();
        }

        /// <summary>
        /// Resolves against the current <see cref="TileSubSystem"/> map services.
        /// Do not cache at Awake — tile map is created later in TileSubSystem.OnStart.
        /// </summary>
        public IStructuralDamageService Service
        {
            get
            {
                EnsureServiceBound();
                return _service;
            }
        }

        public IBlastResolutionService Blast
        {
            get
            {
                EnsureServiceBound();
                return _blast;
            }
        }

        public bool TryApplyStructuralDamage(TileCoord coord, float force, StructuralDamageSource source,
            Vector3 impactPoint = default, Vector3 impactNormal = default)
        {
            IStructuralDamageService service = Service;
            if (service == null)
                return false;

            if (!StructuralIntegrityRules.TryGetTarget(_boundQuery, coord, out PlacedTileObject placed))
                return false;

            bool hasSurface = impactNormal.sqrMagnitude > 0.0001f;
            bool showParticles = hasSurface && SteelWallDamageVfx.SupportsOverlay(placed);
            StructuralMaterial material = StructuralMaterials.Resolve(placed.tileObjectSO);
            StructuralIntegrityStage before = placed.IntegrityStage;
            Vector3 point = hasSurface ? impactPoint : placed.transform.position + Vector3.up;

            if (!service.TryApplyStructuralDamage(coord, force, source))
                return false;

            StructuralIntegrityStage after = placed == null ? StructuralIntegrityStage.Destroyed : placed.IntegrityStage;
            StructuralImpactCue cue = StructuralImpactCues.Resolve(before, after, material, source);
            if ((showParticles || cue != StructuralImpactCue.None)
                && SubSystems.TryGet(out TileSubSystem tiles) && tiles.IsServer)
            {
                tiles.ServerNotifyStructuralImpact(point, showParticles ? impactNormal : Vector3.zero, material, cue);
            }

            return true;
        }

        public bool TryGetIntegrity(TileCoord coord, out StructuralIntegrityStage stage, out float remaining, out float max)
        {
            stage = StructuralIntegrityStage.Intact;
            remaining = 0f;
            max = 0f;

            IStructuralDamageService service = Service;
            return service != null && service.TryGetIntegrity(coord, out stage, out remaining, out max);
        }

        /// <summary>
        /// Server blast BFS from <paramref name="epicenter"/>. No-op if tile services are unbound.
        /// Broadcasts client VFX via <see cref="TileSubSystem"/> after resolution.
        /// </summary>
        public void ResolveBlast(TileCoord epicenter, float yield, float falloff)
        {
            IBlastResolutionService blast = Blast;
            if (blast == null)
                return;

            blast.Resolve(epicenter, yield, falloff);

            if (_boundQuery == null
                || !SubSystems.TryGet(out TileSubSystem tiles)
                || !tiles.IsServer)
            {
                return;
            }

            Vector3 world = _boundQuery.TileToWorld(epicenter);
            tiles.ServerNotifyBlastDetonated(world, yield);
        }

        private void EnsureServiceBound()
        {
            if (!SubSystems.TryGet(out TileSubSystem tiles)
                || tiles.QueryService == null
                || tiles.Construction == null)
            {
                _service = null;
                _blast = null;
                _boundQuery = null;
                _boundConstruction = null;
                return;
            }

            if (_service != null
                && _blast != null
                && ReferenceEquals(_boundQuery, tiles.QueryService)
                && ReferenceEquals(_boundConstruction, tiles.Construction))
            {
                return;
            }

            _boundQuery = tiles.QueryService;
            _boundConstruction = tiles.Construction;
            _service = new StructuralDamageService(_boundQuery, _boundConstruction);
            _blast = new BlastResolutionService(_boundQuery, _service, ApplyCrewBlastBrute);
        }

        private void ApplyCrewBlastBrute(TileCoord coord, float force)
        {
            if (force < BlastResolutionService.MinUsefulForce || _boundQuery == null)
            {
                return;
            }

            var hit = new HashSet<HumanHealthController>();

            if (SubSystems.TryGet(out EntitySubSystem entities) && entities.SpawnedPlayers != null)
            {
                for (int i = 0; i < entities.SpawnedPlayers.Count; i++)
                {
                    Entity entity = entities.SpawnedPlayers[i];
                    if (entity == null)
                    {
                        continue;
                    }

                    HumanHealthController health = entity.GetComponentInChildren<HumanHealthController>();
                    if (health == null || !hit.Add(health))
                    {
                        continue;
                    }

                    TryDamageIfOnTile(health, coord, force);
                }
            }

            HumanHealthController[] all = Object.FindObjectsByType<HumanHealthController>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                HumanHealthController health = all[i];
                if (health == null || !hit.Add(health))
                {
                    continue;
                }

                TryDamageIfOnTile(health, coord, force);
            }
        }

        private void TryDamageIfOnTile(HumanHealthController health, TileCoord coord, float force)
        {
            TileCoord standing = _boundQuery.WorldToTile(health.transform.position, coord.MapId);
            if (standing != coord)
            {
                return;
            }

            health.ApplyDamage(BodyZone.Chest, force, 0f);
        }
    }
}
