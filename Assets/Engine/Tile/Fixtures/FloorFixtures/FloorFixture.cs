using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Tiles
{
    [Serializable]
    public struct FloorFixtureDefinition
    {
        public FurnitureFloorFixture furniture;
        public OverlayFloorFixture overlay1;
        public OverlayFloorFixture overlay2;
        public OverlayFloorFixture overlay3;
        public TableMachineFixture tableMachine1;
        public TableMachineFixture tableMachine2;
        public TableMachineFixture tableMachine3;
        public TableMachineFixture tableMachine4;
        public TableMachineFixture tableMachine5;

        public bool IsEmpty()
        {
            return furniture != null || overlay1 != null || overlay2 != null || overlay3 != null
                || tableMachine1 != null || tableMachine2 != null || tableMachine3 != null || tableMachine4 != null || tableMachine5 != null;
        }
    }

    public enum FloorFixtureLayers
    {
        FurnitureFixture,
        OverlayFixture1,
        OverlayFixture2,
        OverlayFixture3,
        TableMachineFixture1,
        TableMachineFixture2,
        TableMachineFixture3,
        TableMachineFixture4,
        TableMachineFixture5
    }
    
    abstract public class FloorFixture : Fixture
    {

    }
}