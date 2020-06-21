using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Tiles
{
    public enum FloorFixtureLayers
    {
        PipeFixture,
        OverlayFixture1,
        OverlayFixture2,
        OverlayFixture3,
        FurnitureFixture1,
        FurnitureFixture2,
        FurnitureFixture3,
        FurnitureFixture4,
        FurnitureFixture5
    }

    public struct FloorFixtureDefinition
    {
        public PipeFloorFixture pipeFloor;
        public OverlayFloorFixture overlay1;
        public OverlayFloorFixture overlay2;
        public OverlayFloorFixture overlay3;
        public FurnitureFloorFixture furniture1;
        public FurnitureFloorFixture furniture2;
        public FurnitureFloorFixture furniture3;
        public FurnitureFloorFixture furniture4;
        public FurnitureFloorFixture furniture5;
    }

    abstract public class FloorFixture : Fixture
    {

    }
}