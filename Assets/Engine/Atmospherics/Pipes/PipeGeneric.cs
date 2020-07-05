using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public enum PipeLayer
    {
        L1,
        L2,
        L3,
        Upper
    }

    public class PipeGeneric : MonoBehaviour
    {
        protected TileObject[] tileNeighbours = { null, null, null, null };
        protected PipeObject[] atmosNeighbours = { null, null, null, null };
        public PipeLayer layer;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetTileNeighbour(TileObject neighbour, int index)
        {
            tileNeighbours[index] = neighbour;
        }

        public virtual void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                {
                    PipeObject[] pipes = tile.transform.GetComponentsInChildren<PipeObject>();
                    foreach (PipeObject pipe in pipes)
                    {
                        if (pipe.layer == this.layer)
                            atmosNeighbours[i] = pipe;
                    }
                }
                i++;
            }
        }

    }
}