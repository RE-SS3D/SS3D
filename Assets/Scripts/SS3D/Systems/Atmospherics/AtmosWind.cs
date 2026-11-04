using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Pressure-driven wind through the same open edges used by gas sharing. The simulation owns
    /// the field per tile; everything here turns it into something continuous in space and time.
    /// </summary>
    public static class AtmosWind
    {
        public const float KnockdownPressure = 8f;
        public const float MinPressure = 0.5f;
        public const float MaxAcceleration = 35f;

        /// <summary>Wind speed reached at <see cref="SpeedPerPressure"/> × kPa, capped here.</summary>
        public const float MaxSpeed = 7f;
        public const float SpeedPerPressure = 0.6f;

        /// <summary>How quickly a body's felt wind follows the 5 Hz field, per second.</summary>
        public const float ResponseRate = 8f;

        /// <summary>How hard wind drags a ragdoll bone toward wind speed, per second.</summary>
        public const float DragRate = 6f;

        /// <summary>A body stops feeling the last update after this long, covering a dropped tick.</summary>
        public const float HoldSeconds = AtmosConstants.TickInterval * 2.5f;

        public static Vector3 Sample(AtmosSimulation simulation, TileCoord coord) =>
            simulation.TryGetCellIndex(coord, out int index) ? SampleCell(simulation, index) : Vector3.zero;

        /// <summary>
        /// Wind at a world position, blended across the nearest tile centres. Tiles only blend through
        /// open edges, so wind never leaks through a wall or round a sealed corner. Vacuum tiles count
        /// as still air, so wind fades out across the breach instead of stopping at a tile edge.
        /// </summary>
        public static Vector3 SampleAt(AtmosSimulation simulation, Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x);
            int z = Mathf.RoundToInt(world.z);
            if (!simulation.TryGetCellIndex(new TileCoord(simulation.MapId, x, z), out int index)) return Vector3.zero;
            if (simulation.CellMeta[index].State == AtmosCellState.Blocked) return Vector3.zero;

            float offsetX = world.x - x;
            float offsetZ = world.z - z;
            int directionX = offsetX >= 0f ? 1 : 3;
            int directionZ = offsetZ >= 0f ? 0 : 2;
            float weightX = Mathf.Abs(offsetX);
            float weightZ = Mathf.Abs(offsetZ);

            int neighbourX = OpenNeighbour(simulation, index, directionX);
            int neighbourZ = OpenNeighbour(simulation, index, directionZ);
            int diagonal = neighbourX >= 0 ? OpenNeighbour(simulation, neighbourX, directionZ) : -1;
            if (diagonal < 0 && neighbourZ >= 0) diagonal = OpenNeighbour(simulation, neighbourZ, directionX);

            Vector3 wind = Vector3.zero;
            float total = 0f;
            Blend(simulation, index, (1f - weightX) * (1f - weightZ), ref wind, ref total);
            Blend(simulation, neighbourX, weightX * (1f - weightZ), ref wind, ref total);
            Blend(simulation, neighbourZ, (1f - weightX) * weightZ, ref wind, ref total);
            Blend(simulation, diagonal, weightX * weightZ, ref wind, ref total);
            return total > 0f ? wind / total : Vector3.zero;
        }

        public static Vector3 Direction(int index) => index switch
        {
            0 => Vector3.forward, 1 => Vector3.right,
            2 => Vector3.back, _ => Vector3.left,
        };

        /// <summary>The air speed a pressure gradient drives, in metres per second.</summary>
        public static Vector3 Velocity(Vector3 pressureGradient) =>
            Vector3.ClampMagnitude(pressureGradient * SpeedPerPressure, MaxSpeed);

        /// <summary>Frame-rate independent step of a body's felt wind toward the field.</summary>
        public static Vector3 Follow(Vector3 current, Vector3 target, float seconds) =>
            Vector3.Lerp(current, target, 1f - Mathf.Exp(-ResponseRate * seconds));

        /// <summary>
        /// Planar drag on a loose body. Wind only ever pushes: a body already outrunning the air keeps
        /// its speed, so someone blown out of a breach coasts into vacuum instead of being braked.
        /// </summary>
        public static Vector3 DragAcceleration(Vector3 wind, Vector3 bodyVelocity)
        {
            float speed = wind.magnitude;
            if (speed <= 0f) return Vector3.zero;
            Vector3 direction = wind / speed;
            float shortfall = speed - Vector3.Dot(bodyVelocity, direction);
            return direction * Mathf.Min(Mathf.Max(0f, shortfall) * DragRate, MaxAcceleration);
        }

        private static Vector3 SampleCell(AtmosSimulation simulation, int index)
        {
            AtmosCellMeta cell = simulation.CellMeta[index];
            if (cell.State == AtmosCellState.Blocked || cell.State == AtmosCellState.Vacuum)
                return Vector3.zero;
            float pressure = simulation.GetCellPressure(index);
            Vector3 gradient = Vector3.zero;
            for (int d = 0; d < 4; d++)
            {
                int neighbour = simulation.Neighbours[index].Get(d);
                if (neighbour < 0 || simulation.CellMeta[neighbour].State == AtmosCellState.Blocked) continue;
                float drop = pressure - simulation.GetCellPressure(neighbour);
                if (drop <= MinPressure) continue;
                gradient += Direction(d) * drop;
            }
            return gradient;
        }

        private static int OpenNeighbour(AtmosSimulation simulation, int index, int direction)
        {
            int neighbour = simulation.Neighbours[index].Get(direction);
            if (neighbour < 0) return -1;
            return simulation.CellMeta[neighbour].State == AtmosCellState.Blocked ? -1 : neighbour;
        }

        private static void Blend(AtmosSimulation simulation, int index, float weight, ref Vector3 wind, ref float total)
        {
            if (index < 0) return;
            wind += SampleCell(simulation, index) * weight;
            total += weight;
        }
    }
}
