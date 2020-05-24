using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class AtmosManager : MonoBehaviour
    {
        public static int numOfGases = System.Enum.GetNames(typeof(AtmosStates)).Length;

        private TileManager tileManager;
        private List<AtmosObject> atmosTiles;

        // Start is called before the first frame update
        void Start()
        {

        }

        void Update()
        {

        }

        private int Step()
        {
            int activeTiles = 0;

            // Step 1: Calculate flux
            foreach (AtmosObject tile in atmosTiles)
            {
                tile.CalculateFlux();
            }

            // Step 2: Simulate
            foreach (AtmosObject tile in atmosTiles)
            {
                tile.SimulateFlux();
                if (tile.GetState() == AtmosStates.Active)
                    activeTiles++;
            }

            return activeTiles;
        }
    }
}