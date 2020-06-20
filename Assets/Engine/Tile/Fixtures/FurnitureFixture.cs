using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Engine.Tiles
{
    [CreateAssetMenu(fileName = "Furniture", menuName = "Fixtures/Furniture", order = 0)]
    public class FurnitureFixture : Fixture
    {
        public FurnitureFixture()
        {
            layer = FixtureLayers.Furniture;
        }
    }
}