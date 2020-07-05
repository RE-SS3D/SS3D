using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ManifoldObject : PipeObject, IAtmosLoop
    {
        public enum ManifoldType
        {
            FourLayer,
            ThreeLayer
        }

        public ManifoldType manifoldType;

        // We initialize via atmosloop as a device, so that we are sure that all pipes are set up
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

                // For the three layer manifold, we skip the upper pipe layer
                if (manifoldType == ManifoldType.ThreeLayer && pipe.layer == PipeLayer.Upper)
                    continue;

                atmosNeighbours[i] = pipe;
                pipe.ForceNeighbour(this);

                i++;
            }
        }

        // Override as we don't behave like a normal pipe
        public override void SetAtmosNeighbours()
        {

        }

        public void Step()
        {
            return;
        }
    }
}