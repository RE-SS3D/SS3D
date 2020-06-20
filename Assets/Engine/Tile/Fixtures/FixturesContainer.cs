using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Engine.Tiles
{
    // Class that holds all fixtures that can be placed on a tile
    public class FixturesContainer
    {
        public FurnitureFixture furniture;

        public PipesFixture pipe1;
        public PipesFixture pipe2;
        public PipesFixture pipe3;

        public Fixture GetFixtureAtLayer(FixtureLayers layer)
        {
            switch (layer)
            {
                case FixtureLayers.Furniture:
                    return furniture;
                case FixtureLayers.Pipes:
                    // TODO add pipe layers
                    break;

            }
            return null;
        }
    }

    //[Serializable]
    //public struct FixturesHolder
    //{
    //    public FurnitureFixture furniture;

    //    PipesFixture pipe1;
    //    PipesFixture pipe2;
    //    PipesFixture pipe3;

    //    public Fixture GetFixtureAtLayer(FixtureLayers layer)
    //    {
    //        switch(layer)
    //        {
    //            case FixtureLayers.Furniture:
    //                return furniture;
    //            case FixtureLayers.Pipes:
    //                // TODO add pipe layers
    //                break;

    //        }
    //        return null;
    //    }
    //}
}