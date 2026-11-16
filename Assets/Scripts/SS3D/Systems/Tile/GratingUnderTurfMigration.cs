using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Load-time fix for maps saved while walls and floors could sit on a catwalk or lattice. Turf now needs a real
    /// plenum (<see cref="BuildFailReason.TurfNeedsPlenum"/>), so a catwalk or lattice under turf loads as a plenum.
    /// Wires and pipes on the cell are kept. Gratings with no turf above are left alone.
    /// </summary>
    public static class GratingUnderTurfMigration
    {
        /// <summary>Name of the plenum asset a converted grating loads as.</summary>
        public const string PlenumAssetName = "Plenum";

        /// <summary>
        /// Returns the cells whose Plenum-layer catwalk or lattice should load as a plenum, because turf sits on them.
        /// </summary>
        /// <param name="placements">Each saved placement in one chunk: its local cell and its resolved asset.</param>
        public static HashSet<Vector2Int> FindCellsToConvert(IEnumerable<(Vector2Int Cell, TileObjectSo So)> placements)
        {
            var turfCells = new HashSet<Vector2Int>();
            var gratingCells = new HashSet<Vector2Int>();

            foreach ((Vector2Int cell, TileObjectSo so) in placements)
            {
                if (so == null)
                    continue;

                if (so.layer == TileLayer.Turf)
                    turfCells.Add(cell);
                else if (so.layer == TileLayer.Plenum && BuildChecker.IsGrating(so.NameString))
                    gratingCells.Add(cell);
            }

            gratingCells.IntersectWith(turfCells);
            return gratingCells;
        }
    }
}
