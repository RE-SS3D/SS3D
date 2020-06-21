using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    public struct TileFixtureDefinition
    {
        public PlenumFixture plenum;
        public WireFixture wire;
        public DisposalFixture disposal;
        public PipesFixture pipe1;
        public PipesFixture pipe2;
        public PipesFixture pipe3;
    }

    public enum TileFixtureLayers
    {
        Plenum,
        Wire,
        Disposal,
        Pipe1,
        Pipe2,
        Pipe3
    }

    abstract public class TileFixture : Fixture
    {
       
    }
}
