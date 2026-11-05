using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Networking;
using SS3D.Systems.Tile.MapEditor;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Server-only index of airlocks by HashGrid cell. Ticks proximity from players outward
    /// so cost scales with nearby doors, not station door count.
    /// </summary>
    public sealed class AirLockProximityService : MonoBehaviour
    {
        private static readonly ProfilerMarker NearbyTickPerformanceMarker = new("SS3D.Airlock.NearbyTick");
        private static readonly ProfilerMarker EmptyPassPerformanceMarker = new("SS3D.Airlock.EmptyPassSweep");

        private static AirLockProximityService _instance;

        private readonly Dictionary<Vector2Int, List<AirLockOpener>> _byCell = new();
        private readonly Dictionary<AirLockOpener, Vector2Int> _openerCells = new();
        private readonly HashSet<AirLockOpener> _pendingEmptyPass = new();
        private readonly HashSet<AirLockOpener> _visitedScratch = new();
        private readonly List<AirLockOpener> _nearbyScratch = new();
        private readonly List<AirLockOpener> _emptyPassScratch = new();

        public static AirLockProximityService Instance
        {
            get
            {
                if (_instance == null)
                    EnsureExists();

                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (_instance != null)
                return;

            var go = new GameObject(nameof(AirLockProximityService));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AirLockProximityService>();
        }

        public void Register(AirLockOpener opener)
        {
            if (opener == null || _openerCells.ContainsKey(opener))
                return;

            Vector2Int cell = TileObserverConstants.GetHashGridCell(opener.transform.position);
            _openerCells[opener] = cell;
            if (!_byCell.TryGetValue(cell, out List<AirLockOpener> list))
            {
                list = new List<AirLockOpener>(4);
                _byCell[cell] = list;
            }

            list.Add(opener);
            RefreshEmptyPassMembership(opener);
        }

        public void Unregister(AirLockOpener opener)
        {
            if (opener == null || !_openerCells.TryGetValue(opener, out Vector2Int cell))
                return;

            _openerCells.Remove(opener);
            _pendingEmptyPass.Remove(opener);
            if (_byCell.TryGetValue(cell, out List<AirLockOpener> list))
            {
                list.Remove(opener);
                if (list.Count == 0)
                    _byCell.Remove(cell);
            }
        }

        /// <summary>EditMode / tests: doors currently indexed.</summary>
        public int RegisteredCount => _openerCells.Count;

        /// <summary>EditMode / tests: doors queued for an empty proximity pass.</summary>
        public int PendingEmptyPassCount => _pendingEmptyPass.Count;

        /// <summary>EditMode / tests: collect openers in the 3×3 HashGrid neighborhood of <paramref name="worldPosition"/>.</summary>
        public void CollectNearby(Vector3 worldPosition, List<AirLockOpener> results)
        {
            results.Clear();
            Vector2Int center = TileObserverConstants.GetHashGridCell(worldPosition);
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    Vector2Int cell = new Vector2Int(center.x + dx, center.y + dz);
                    if (!_byCell.TryGetValue(cell, out List<AirLockOpener> list))
                        continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        AirLockOpener opener = list[i];
                        if (opener != null)
                            results.Add(opener);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (NetworkManager.Instances.Count == 0 || !InstanceFinder.IsServer || _openerCells.Count == 0)
                return;

            if (SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive)
                return;

            if (!SubSystems.TryGet(out EntitySubSystem entities))
                return;

            IReadOnlyList<Entity> players = entities.SpawnedPlayers;
            _visitedScratch.Clear();

            if (players.Count > 0)
            {
                using (NearbyTickPerformanceMarker.Auto())
                {
                    for (int p = 0; p < players.Count; p++)
                    {
                        Entity entity = players[p];
                        if (entity == null)
                            continue;

                        CollectNearby(entity.transform.position, _nearbyScratch);
                        for (int i = 0; i < _nearbyScratch.Count; i++)
                        {
                            AirLockOpener opener = _nearbyScratch[i];
                            if (opener == null || !_visitedScratch.Add(opener))
                                continue;

                            opener.ServerUpdateProximityFromService(players);
                            RefreshEmptyPassMembership(opener);
                        }
                    }
                }
            }

            if (_pendingEmptyPass.Count == 0)
                return;

            // Copy before mutate: proximity updates clear occupants and drop membership.
            _emptyPassScratch.Clear();
            foreach (AirLockOpener opener in _pendingEmptyPass)
                _emptyPassScratch.Add(opener);

            using (EmptyPassPerformanceMarker.Auto())
            {
                for (int i = 0; i < _emptyPassScratch.Count; i++)
                {
                    AirLockOpener opener = _emptyPassScratch[i];
                    if (opener == null || _visitedScratch.Contains(opener))
                        continue;

                    opener.ServerUpdateProximityFromService(players);
                    RefreshEmptyPassMembership(opener);
                }
            }
        }

        private void RefreshEmptyPassMembership(AirLockOpener opener)
        {
            if (opener == null || !_openerCells.ContainsKey(opener))
            {
                if (opener != null)
                    _pendingEmptyPass.Remove(opener);
                return;
            }

            if (opener.NeedsEmptyProximityPass)
                _pendingEmptyPass.Add(opener);
            else
                _pendingEmptyPass.Remove(opener);
        }
    }
}
