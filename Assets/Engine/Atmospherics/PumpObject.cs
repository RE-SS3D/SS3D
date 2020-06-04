using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class PumpObject : MonoBehaviour
    {
        private TileObject[] tileNeighbours = { null, null };
        private PipeObject[] atmosNeighbours = { null, null };

        private const float maxPressureSetting = 4500f;
        private float currentPressureSetting = 1000f;
        private bool pumpActive= false;
        private float[] tempGasses = new float[Gas.numOfGases];

        void Start()
        {

        }

        void Update()
        {

        }

        public void Step()
        {
            // If both sides of the pump are connected
            if (pumpActive && atmosNeighbours[0] && atmosNeighbours[1])
            {
                PipeObject input = atmosNeighbours[0];
                PipeObject output = atmosNeighbours[1];

                // And the output pressure is acceptable
                if (output.GetPressure() < currentPressureSetting - 100f)
                {
                    tempGasses = input.GetAtmosContainer().GetGasses();
                }
            }
        }

        public void setTileNeighbour(TileObject neighbour, int index)
        {
            tileNeighbours[index] = neighbour;
        }

        public void setPipeNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                    atmosNeighbours[i] = tile.transform.GetComponentInChildren<PipeObject>();
                i++;
            }
        }

        public void SetActive(bool pumpActive)
        {
            this.pumpActive = pumpActive;
        }

    }
}