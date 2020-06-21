using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    // Fixture holder for under floor pipes
    [CreateAssetMenu(fileName = "PipeTileFixture", menuName = "Fixtures/Tile/Under Floor Pipe", order = 0)]
    public class PipesFixture : TileFixture
    {
        // TODO

        //public enum PipeLayers
        //{
        //    Pipes_1,
        //    Pipes_2,
        //    Pipes_3,
        //}

        //public PipeLayers pipeLayer { get; set; }

        public PipesFixture()
        {
            layer = FixtureLayers.Pipes1;
        }
    }
}
