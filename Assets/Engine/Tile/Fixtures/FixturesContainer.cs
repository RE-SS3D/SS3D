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
                case FixtureLayers.Pipes1:
                    return pipe1;
                case FixtureLayers.Pipes2:
                    return pipe2;
                case FixtureLayers.Pipes3:
                    return pipe3;

            }
            return null;
        }

        public void SetFixtureAtLayer(Fixture fixture, FixtureLayers layer)
        {
            switch (layer)
            {
                case FixtureLayers.Furniture:
                    furniture = (FurnitureFixture)fixture;
                    break;
                case FixtureLayers.Pipes1:
                    pipe1 = (PipesFixture)fixture;
                    break;
                case FixtureLayers.Pipes2:
                    pipe2 = (PipesFixture)fixture;
                    break;
                case FixtureLayers.Pipes3:
                    pipe3 = (PipesFixture)fixture;
                    break;
            }
        }

        public Fixture[] GetAllFixtures()
        {
            return new Fixture[] { furniture, pipe1, pipe2, pipe3 };
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