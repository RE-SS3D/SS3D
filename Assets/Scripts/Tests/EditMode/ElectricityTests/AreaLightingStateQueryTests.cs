using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class AreaLightingStateQueryTests
    {
        [Test]
        public void TryGetLightingStateForTile_ReturnsStateForAssignedTile()
        {
            AreaLightingHarness harness = AreaLightingHarness.Create();
            TileCoord coord = new TileCoord(harness.Map.MapId, 12, 12);
            harness.SetLightingState(harness.DefaultAreaId, AreaLightingState.Emergency);

            Assert.IsTrue(harness.AreaSubSystem.TryGetLightingStateForTile(coord, out AreaLightingState state));
            Assert.AreEqual(AreaLightingState.Emergency, state);
        }

        [Test]
        public void TryGetLightingStateForTile_ReturnsFalseForUnassignedTile()
        {
            AreaLightingHarness harness = AreaLightingHarness.Create();
            TileCoord coord = new TileCoord(harness.Map.MapId, 99, 99);

            Assert.IsFalse(harness.AreaSubSystem.TryGetLightingStateForTile(coord, out _));
        }

        private sealed class AreaLightingHarness
        {
            private AreaLightingHarness(TileMap map, AreaSubSystemHarness areaSubSystem)
            {
                Map = map;
                AreaSubSystem = areaSubSystem;
                DefaultAreaId = areaSubSystem.DefaultAreaId;
            }

            public TileMap Map { get; }

            public AreaSubSystemHarness AreaSubSystem { get; }

            public AreaId DefaultAreaId { get; }

            public static AreaLightingHarness Create()
            {
                TileMapTestUtilities.EnsureTestAssetsRegistered();
                TileMap map = TileMap.Create("AreaLightingStateQueryTests");
                var query = new TileQueryService(map);
                var areaSubSystem = new AreaSubSystemHarness(map, query);
                areaSubSystem.AssignAreaToTile(new TileCoord(map.MapId, 12, 12), areaSubSystem.DefaultAreaId);
                return new AreaLightingHarness(map, areaSubSystem);
            }

            public void SetLightingState(AreaId areaId, AreaLightingState state) =>
                AreaSubSystem.SetLightingState(areaId, state);
        }

        private sealed class AreaSubSystemHarness : IAreaLightingStateSource
        {
            private readonly TileMap _map;
            private readonly TileQueryService _query;
            private readonly Dictionary<AreaId, AreaRecord> _records = new();
            private readonly Dictionary<AreaId, AreaLightingState> _lightingStates = new();

            public AreaSubSystemHarness(TileMap map, TileQueryService query)
            {
                _map = map;
                _query = query;
                DefaultAreaId = new AreaId(1);
                _records[DefaultAreaId] = new AreaRecord
                {
                    Id = DefaultAreaId,
                    DisplayName = "Test Area",
                };
            }

            public AreaId DefaultAreaId { get; }

            public bool TryGetLightingState(AreaId areaId, out AreaLightingState state) =>
                _lightingStates.TryGetValue(areaId, out state);

            public bool TryGetLightingStateForTile(TileCoord coord, out AreaLightingState state)
            {
                state = default;
                if (!_map.TryGetAreaId(coord, out ushort areaId))
                {
                    return false;
                }

                return TryGetLightingState(new AreaId(areaId), out state);
            }

            public bool TryGetAreaForTile(TileCoord coord, out AreaRecord record)
            {
                record = null;
                if (!_map.TryGetAreaId(coord, out ushort areaId))
                {
                    return false;
                }

                return _records.TryGetValue(new AreaId(areaId), out record);
            }

            public void AssignAreaToTile(TileCoord coord, AreaId areaId) =>
                _map.TrySetAreaId(coord, areaId.Value);

            public void SetLightingState(AreaId areaId, AreaLightingState state) =>
                _lightingStates[areaId] = state;
        }
    }
}
