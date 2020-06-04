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
        private const float molesPerStep = 5f;

        private float currentPressureSetting = 1000f;
        public bool pumpActive= false;

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
                    float[] inputGasses = input.GetAtmosContainer().GetGasses();
                    float[] outputGasses = output.GetAtmosContainer().GetGasses();

                    for (int i=0; i < Gas.numOfGases; i++)
                    {
                        if (inputGasses[i] >= molesPerStep)
                        {
                            inputGasses[i] -= molesPerStep;
                            outputGasses[i] += molesPerStep;
                            input.SetStateActive();
                            output.SetStateActive();
                        }
                    }
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