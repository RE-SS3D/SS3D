using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Engine.Tiles
{
    public class FixturesContainer : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

    [Serializable]
    public struct FixturesHolder
    {
        FurnitureFixture furniture;

        PipesFixture pipe1;
        PipesFixture pipe2;
        PipesFixture pipe3;
    }
}