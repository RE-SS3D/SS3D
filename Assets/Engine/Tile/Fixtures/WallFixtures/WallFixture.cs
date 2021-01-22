using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    [Serializable]
    public struct WallFixtureDefinition
    {
        public HighWallFixture highWallNorth;
        public HighWallFixture highWallEast;
        public HighWallFixture highWallSouth;
        public HighWallFixture highWallWest;
        public LowWallFixture lowWallNorth;
        public LowWallFixture lowWallEast;
        public LowWallFixture lowWallSouth;
        public LowWallFixture lowWallWest;
    }

    public enum WallFixtureLayers
    {
        HighWallNorth,
        HighWallEast,
        HighWallSouth,
        HighWallWest,
        LowWallNorth,
        LowWallEast,
        LowWallSouth,
        LowWallWest
    }

    [Serializable]
    abstract public class WallFixture : Fixture
    {

    }
}