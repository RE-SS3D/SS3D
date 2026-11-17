using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
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
        private IReadOnlyDictionary<TileCoord, float> _lastBlastForces;
        private ITileQueryService _boundQuery;
        private IConstructionService _boundConstruction;

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
        /// Announces the blast via <see cref="TileSubSystem"/> before resolution (debris direction) and broadcasts the
        /// detonation VFX, with the tiles it reached, after.
        /// </summary>
        public void ResolveBlast(TileCoord epicenter, float yield, float falloff, bool smallSound = false)
        {
            IBlastResolutionService blast = Blast;
            if (blast == null)
                return;

            TileSubSystem tiles = Announce(epicenter, falloff > 0f ? yield / falloff : yield);
            _lastBlastForces = null;
            blast.Resolve(epicenter, yield, falloff);
            LogBlast(epicenter, yield);
            tiles?.ServerNotifyBlastDetonated(_boundQuery.TileToWorld(epicenter), yield, smallSound,
                BlastFlameTiles.Pack(epicenter, _lastBlastForces));
        }

        /// <summary>X4's stronger radial blast plus extra force beyond its attached target; one audiovisual event.</summary>
        public void ResolveShapedCharge(TileCoord target, Vector2 direction)
        {
            EnsureServiceBound();
            if (_blast == null) return;
            TileSubSystem tiles = Announce(target, Mathf.Max(240f / 60f, 180f / 40f));
            _lastBlastForces = null;
            _blast.ResolveShaped(target, 240f, 60f, 180f, 40f, direction);
            LogBlast(target, 240f);
            tiles?.ServerNotifyBlastDetonated(_boundQuery.TileToWorld(target), 300f, flameTiles: BlastFlameTiles.Pack(target, _lastBlastForces));
        }

        private void LogBlast(TileCoord epicenter, float yield)
        {
            int destroyed = _service?.TakeBlastDestroyedCount() ?? 0;
            Log.Information(this, "Blast at {epicenter} (yield {yield}) destroyed {destroyed} structures, reached {tiles} tiles",
                Logs.Generic, epicenter, yield, destroyed, _lastBlastForces?.Count ?? 0);
        }

        /// <summary>Sends the blast-started notice; null when there is no server tile system to send through.</summary>
        private TileSubSystem Announce(TileCoord epicenter, float reach)
        {
            if (_boundQuery == null || !SubSystems.TryGet(out TileSubSystem tiles) || !tiles.IsServer)
                return null;

            tiles.ServerNotifyBlastStarted(_boundQuery.TileToWorld(epicenter), reach);
            return tiles;
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
            _blast = new BlastResolutionService(_boundQuery, _service, ApplyCrewBlastBrute, _service.ApplyBlastFloorDamage, ApplyCrewBlastPush);
        }

        /// <summary>
        /// Called once per tile with the blast's final force there (<see cref="BlastResolutionService"/>), so a body takes
        /// one hit per detonation. Bodies come from <see cref="HumanHealthController.ServerInstances"/>, not a scene scan.
        /// </summary>
        private void ApplyCrewBlastBrute(TileCoord coord, float force)
        {
            if (force < BlastResolutionService.MinUsefulForce || _boundQuery == null)
            {
                return;
            }

            foreach (HumanHealthController health in HumanHealthController.ServerInstances)
            {
                if (health != null)
                {
                    TryDamageIfOnTile(health, coord, force);
                }
            }
        }

        /// <summary>Runs once all passes are done, so a body gets one push from one detonation.</summary>
        private void ApplyCrewBlastPush(TileCoord epicenter, IReadOnlyDictionary<TileCoord, float> forces)
        {
            // The detonation RPC sends these tiles as flames once the blast has finished resolving.
            _lastBlastForces = forces;
            if (_boundQuery == null)
            {
                return;
            }

            Vector3 origin = _boundQuery.TileToWorld(epicenter);
            foreach (Ragdoll body in Ragdoll.WindBodies)
            {
                if (body == null || !body.IsServer)
                {
                    continue;
                }

                Vector3 position = body.WindSamplePosition;
                if (!forces.TryGetValue(_boundQuery.WorldToTile(position, epicenter.MapId), out float force))
                {
                    continue;
                }

                body.ServerApplyBlast(BlastPush.Velocity(origin, position, -body.transform.forward, force), BlastPush.KnocksDown(force));
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
