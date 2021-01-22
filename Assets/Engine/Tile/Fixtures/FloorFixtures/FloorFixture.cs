using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Tiles
{
    [Serializable]
    public struct FloorFixtureDefinition
    {
        public PipeFloorFixture pipeUpper;
        public OverlayFloorFixture overlay1;
        public OverlayFloorFixture overlay2;
        public OverlayFloorFixture overlay3;
        public FurnitureFloorFixture furnitureMain;
        public FurnitureFloorFixture furniture2;
        public FurnitureFloorFixture furniture3;
        public FurnitureFloorFixture furniture4;
        public FurnitureFloorFixture furniture5;

        public bool IsEmpty()
        {
            // Important: pipe upper is not checked as it can exist without a floor turf in place
            return overlay1 != null || overlay2 != null || overlay3 != null
                || furnitureMain != null || furniture2 != null || furniture3 != null || furniture4 != null || furniture5 != null;
        }
    }

    public enum FloorFixtureLayers
    {
        PipeUpperFixture,
        OverlayFixture1,
        OverlayFixture2,
        OverlayFixture3,
        FurnitureFixtureMain,
        FurnitureFixture2,
        FurnitureFixture3,
        FurnitureFixture4,
        FurnitureFixture5
    }
    
    abstract public class FloorFixture : Fixture
    {

    }
}