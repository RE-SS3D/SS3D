using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Tiles
{
    [Serializable]
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

        public bool IsEmpty()
        {
            return pipeFloor != null || overlay1 != null || overlay2 != null || overlay3 != null
                || furniture1 != null || furniture2 != null || furniture3 != null || furniture4 != null || furniture5 != null;
        }
    }

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
    
    abstract public class FloorFixture : Fixture
    {

    }
}