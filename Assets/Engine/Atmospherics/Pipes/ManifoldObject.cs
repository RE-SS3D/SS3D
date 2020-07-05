using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ManifoldObject : PipeObject
    {
        public override void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                {
                    PipeObject[] pipes = tile.transform.GetComponentsInChildren<PipeObject>();
                    foreach (PipeObject pipe in pipes)
                    {
                        if (i < 4)
                        {
                            atmosNeighbours[i] = pipe;
                            pipe.ForceNeighbour(this);
                        }
                        else
                            Debug.LogError("Atmos neighbour out of range");
                        i++;
                    }
                }

            }
        }
    }
}