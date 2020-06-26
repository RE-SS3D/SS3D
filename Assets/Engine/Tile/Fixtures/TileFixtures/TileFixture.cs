using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    [Serializable]
    public struct TileFixtureDefinition
    {
        public WireFixture wire;
        public DisposalFixture disposal;
        public PipeFixture pipe1;
        public PipeFixture pipe2;
        public PipeFixture pipe3;
    }

    public enum TileFixtureLayers
    {
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
