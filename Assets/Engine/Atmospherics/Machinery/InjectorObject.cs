using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class InjectorObject : PipeGeneric, IAtmosLoop
    {
        private TileObject tileNeighbour;
        private PipeObject atmosNeighbour;
        private TileManager tileManager;
        private TileObject tile;

        public void Initialize()
        {
            tile = GetComponentInParent<TileObject>();
            SetAtmosNeighbours();
        }

        public void Step()
        {
            if (atmosNeighbour)
            {
                if (atmosNeighbour.GetTotalMoles() > 0.1f)
                {
                    float[] tileGas = tile.atmos.GetAtmosContainer().GetGasses();
                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        float gasToTransfer = atmosNeighbour.GetAtmosContainer().GetGas(i);
                        tile.atmos.AddGas(i, gasToTransfer);
                        atmosNeighbour.RemoveGas(i, gasToTransfer);
                    }
                }
            }
        }
    }
}