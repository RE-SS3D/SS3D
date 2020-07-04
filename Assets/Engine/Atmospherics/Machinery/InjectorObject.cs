using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class InjectorObject : MonoBehaviour, IAtmosLoop
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

        public void SetTileNeighbour(TileObject tile, int index)
        {
            // We only connect the North side
            if (index == 0)
            {
                tileNeighbour = tile;
            }
        }

        public void SetAtmosNeighbours()
        {
            atmosNeighbour = tileNeighbour?.transform.GetComponentInChildren<PipeObject>();
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