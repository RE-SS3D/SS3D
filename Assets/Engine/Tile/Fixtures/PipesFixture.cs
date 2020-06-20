using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    [CreateAssetMenu(fileName = "Pipes", menuName = "Fixtures/Pipes", order = 0)]
    public class PipesFixture : Fixture
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
