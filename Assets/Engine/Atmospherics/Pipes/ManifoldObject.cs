using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ManifoldObject : PipeObject, IAtmosLoop
    {
        public void Initialize()
        {
            // We only check the pipes that are on our own tile
            TileObject tileObject = GetComponentInParent<TileObject>();
            PipeObject[] pipes = tileObject.GetComponentsInChildren<PipeObject>();

            int i = 0;
            foreach (PipeObject pipe in pipes)
            {
                // Skip ourselves
                if (pipe.name == this.name)
                    continue;

                atmosNeighbours[i] = pipe;
                pipe.ForceNeighbour(this);

                i++;
            }
        }

        public override void SetAtmosNeighbours()
        {
            

            //int i = 0;
            //// We only accept a North connection
            //for (int j = 0; j < 2; j += 2)
            //{
            //    if (atmosNeighbours[j] != null)
            //    {
            //        PipeObject[] pipes = atmosNeighbours[j].transform.GetComponentsInChildren<PipeObject>();
            //        foreach (PipeObject pipe in pipes)
            //        {
            //            if (i < atmosNeighbours.Length)
            //            {
            //                atmosNeighbours[i] = pipe;
            //                pipe.ForceNeighbour(this);
            //            }
            //            else
            //                Debug.LogError("Atmos neighbour out of range");
            //            i++;
            //        }
            //    }

            //}

        }

        public void Step()
        {
            return;
        }
    }
}