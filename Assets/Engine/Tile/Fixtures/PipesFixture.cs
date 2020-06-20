using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    [CreateAssetMenu(fileName = "Pipes", menuName = "Fixtures/Pipes", order = 0)]
    public class PipesFixture : Fixture
    {
        public enum PipeLayers
        {
            Pipes_1,
            Pipes_2,
            Pipes_3,
        }

        public PipeLayers pipeLayer;

        public PipesFixture()
        {
            layer = FixtureLayers.Pipes;
        }
    }
}
