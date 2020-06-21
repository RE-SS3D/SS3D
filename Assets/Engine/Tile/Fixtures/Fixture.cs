using UnityEngine;
using System.Collections;
using System;

namespace SS3D.Engine.Tiles {

    /**
     * Describes the different layers on which fixtures can be placed.
     */
    //public enum FixtureLayers
    //{
    //    Furniture,
    //    Electrical,
    //    Disposal,
    //    Pipes1,
    //    Pipes2,
    //    Pipes3
    //}

    public enum FixtureType
    {
        TileFixture,
        WallFixture,
        FloorFixture
    }

    /**
     * Describes an object that is attached to a turf.
     */
    public class Fixture : ScriptableObject
    {
        public string id;
        public string genericType;

        protected FixtureType fixtureType;
        // protected FixtureLayers layer;
        public GameObject prefab;
    }
}
