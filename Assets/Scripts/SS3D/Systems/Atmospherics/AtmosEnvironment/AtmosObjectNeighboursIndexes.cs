using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Lil struct containing the atmos indexes of the 4 neighbours of a given atmos tile.
    /// </summary>
    public struct AtmosObjectNeighboursIndexes
    {
        public AtmosObjectNeighboursIndexes(int northNeighbour, int southNeighbour, int eastNeighbour, int westNeighbour)
        {
            NorthNeighbour = northNeighbour;
            SouthNeighbour = southNeighbour;
            EastNeighbour = eastNeighbour;
            WestNeighbour = westNeighbour;
            NeighbourCount = 0;

            if (northNeighbour != -1)
            {
                NeighbourCount++;
            }

            if (southNeighbour != -1)
            {
                NeighbourCount++;
            }

            if (eastNeighbour != -1)
            {
                NeighbourCount++;
            }

            if (westNeighbour != -1)
            {
                NeighbourCount++;
            }
        }

        public int NorthNeighbour { get; }

        public int SouthNeighbour { get; }

        public int EastNeighbour { get; }

        public int WestNeighbour { get; }

        public int NeighbourCount { get; }
    }
}
