using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// The air and wind where the listener stands, from the atlas this client already has, so it adds
    /// no messages. The atlas moves in 5 Hz steps; this follows it smoothly.
    /// </summary>
    public sealed class ListenerAirTracker
    {
        private const float AirFollowRate = 2f;
        private const float WindFollowRate = 4f;

        /// <summary>Reach of the check for station around the listener, in tiles.</summary>
        private const int HullReach = 6;
        private const int HullProbeStep = 3;

        /// <summary>How well the air here carries sound, 0..1.</summary>
        public float Air { get; private set; } = 1f;

        /// <summary>Wind where the listener stands, in m/s.</summary>
        public float WindSpeed { get; private set; }

        public void Follow(Vector3 listener, bool inOpenSpace, float seconds)
        {
            float targetAir;
            float targetWind = 0f;

            if (AtmosRenderContext.IsRenderingSuppressed)
            {
                targetAir = 1f;
            }
            else if (AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot))
            {
                AtmosCellVisualEncoding.CellRead read = AtmosCellVisualEncoding.ReadCell(
                    snapshot.PressureData, snapshot.MaskData, snapshot.AtlasBounds, listener.x, listener.z,
                    out float pressureKpa);
                targetAir = AudioAirState.ResolveAir(read, pressureKpa, Air, inOpenSpace);
                targetWind = AtmosCellVisualEncoding.DecodeWindSpeed(AtmosCellVisualEncoding.SampleFlow(
                    snapshot.FlowData, snapshot.AtlasBounds, listener.x, listener.z));
            }
            else
            {
                targetAir = AudioAirState.ResolveAir(AtmosCellVisualEncoding.CellRead.Unknown, 0f, Air, inOpenSpace);
            }

            Air = Mathf.Lerp(Air, targetAir, 1f - Mathf.Exp(-AirFollowRate * seconds));
            WindSpeed = Mathf.Lerp(WindSpeed, targetWind, 1f - Mathf.Exp(-WindFollowRate * seconds));
        }

        public void Reset()
        {
            Air = 1f;
            WindSpeed = 0f;
        }

        /// <summary>
        /// Whether station lies near the listener. Reads loaded tiles and not the atlas, because a pure
        /// client's atlas can be missing the hull it's floating beside.
        /// </summary>
        public static bool IsNearStation(Vector3 listener)
        {
            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null || tiles.QueryService == null)
            {
                return false;
            }

            ITileQueryService query = tiles.QueryService;
            TileCoord centre = query.WorldToTile(listener, tiles.CurrentMap.MapId);
            for (int x = -HullReach; x <= HullReach; x += HullProbeStep)
            {
                for (int z = -HullReach; z <= HullReach; z += HullProbeStep)
                {
                    var coord = new TileCoord(centre.MapId, centre.Grid.x + x, centre.Grid.y + z);
                    if (query.TryGetOccupancy(coord, out TileOccupancy occupancy)
                        && (occupancy.HasPlenum || occupancy.HasTurf || occupancy.HasWall))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
