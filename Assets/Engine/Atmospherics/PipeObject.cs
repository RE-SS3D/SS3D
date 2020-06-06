using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class PipeObject : MonoBehaviour
    {
        private const float maxPipePressure = 2000f;

        public float volume = 1f;

        private AtmosContainer atmosContainer = new AtmosContainer();
        private float[] tileFlux = { 0f, 0f, 0f, 0f };
        private AtmosStates state = AtmosStates.Active;
        private TileObject[] tileNeighbours = { null, null, null, null };
        private PipeObject[] atmosNeighbours = { null, null, null, null };
        private bool tempSetting = false;
        private bool[] activeDirection = {
                false,  // Top AtmosObject active
                false,  // Bottom AtmosObject active
                false,  // Left AtmosObject active
                false   // Right AtmosObject active
            };

        private float[] gasDifference = new float[Gas.numOfGases];
        private float[] neighbourFlux = new float[4];

        private void Start()
        {
            atmosContainer.Volume = volume;
        }

        public AtmosContainer GetAtmosContainer()
        {
            return atmosContainer;
        }

        public void SetTileNeighbour(TileObject neighbour, int index)
        {
            tileNeighbours[index] = neighbour;
        }

        public void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                    atmosNeighbours[i] = tile.transform.GetComponentInChildren<PipeObject>();
                i++;
            }
        }

        public AtmosStates GetState()
        {
            return state;
        }

        public void SetStateActive()
        {
            state = AtmosStates.Active;
        }

        public void SetBlocked(bool blocked)
        {
            if (!blocked)
            {
                state = AtmosStates.Active;
            }
            else
            {
                state = AtmosStates.Blocked;
            }
        }

        public void RemoveFlux()
        {
            tileFlux = new float[] { 0f, 0f, 0f, 0f };
        }

        public void AddGas(AtmosGasses gas, float amount)
        {
            if (state != AtmosStates.Blocked)
            {
                atmosContainer.AddGas(gas, amount);
                state = AtmosStates.Active;
            }
        }

        public void SetGasses(float[] amounts)
        {
            atmosContainer.SetGasses(amounts);
            state = AtmosStates.Active;
        }

        public void MakeEmpty()
        {
            atmosContainer.MakeEmpty();
        }

        public void AddHeat(float temp)
        {
            atmosContainer.AddHeat(temp);
            state = AtmosStates.Active;
        }

        public void RemoveHeat(float temp)
        {
            atmosContainer.RemoveHeat(temp);
            state = AtmosStates.Active;
        }

        public float GetTotalMoles()
        {
            return atmosContainer.GetTotalMoles();
        }

        public float GetPressure()
        {
            return atmosContainer.GetPressure();
        }

        public float GetPartialPressure(int index)
        {
            return atmosContainer.GetPartialPressure(index);
        }

        public float GetPartialPressure(AtmosGasses gas)
        {
            return atmosContainer.GetPartialPressure(gas);
        }

        public bool CheckOverPressure()
        {
            if (atmosContainer.GetPressure() >= maxPipePressure)
            {
                return true;
            }
            return false;
        }

        public void CalculateFlux()
        {
            Array.Clear(neighbourFlux, 0, neighbourFlux.Length);
            int i = 0;
            float pressure = GetPressure();
            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe != null && pipe.state != AtmosStates.Blocked)
                {
                    neighbourFlux[i] = Mathf.Min(tileFlux[i] * Gas.drag + (pressure - pipe.GetPressure()) * Gas.dt, 1000f);
                    activeDirection[i] = true;

                    if (neighbourFlux[i] < 0f)
                    {
                        pipe.state = AtmosStates.Active;
                        neighbourFlux[i] = 0f;
                    }
                }

                i++;
            }

            if (neighbourFlux[0] > Gas.fluxEpsilon || neighbourFlux[1] > Gas.fluxEpsilon || neighbourFlux[2] > Gas.fluxEpsilon ||
                neighbourFlux[3] > Gas.fluxEpsilon)
            {
                float scalingFactor = Mathf.Min(1,
                    pressure / (neighbourFlux[0] + neighbourFlux[1] + neighbourFlux[2] + neighbourFlux[3]) / Gas.dt);

                for (int j = 0; j < 4; j++)
                {
                    neighbourFlux[j] *= scalingFactor;
                    tileFlux[j] = neighbourFlux[j];
                }
            }
            else
            {
                for (int j = 0; j < 4; j++)
                {
                    tileFlux[j] = 0;
                }

                if (!tempSetting)
                {
                    state = AtmosStates.Semiactive;
                }
                else
                {
                    tempSetting = false;
                }
            }
        }

        public void SimulateFlux()
        {
            float[] gasses = atmosContainer.GetGasses();

            if (state == AtmosStates.Active)
            {
                float pressure = GetPressure();

                for (int i = 0; i < Gas.numOfGases; i++)
                {
                    if (gasses[i] < 1f)
                        gasses[i] = 0f;

                    if (gasses[i] > 0f)
                    {
                        int k = 0;
                        foreach (PipeObject pipe in atmosNeighbours)
                        {
                            if (tileFlux[k] > 0f)
                            {
                                float factor = gasses[i] * (tileFlux[k] / pressure);
                                if (pipe.state != AtmosStates.Vacuum)
                                {
                                    pipe.atmosContainer.GetGasses()[i] += factor;
                                    pipe.state = AtmosStates.Active;
                                }
                                else
                                {
                                    activeDirection[k] = false;
                                }
                                gasses[i] -= factor;
                            }
                            k++;
                        }
                    }
                }

                float difference;
                int j = 0;
                foreach (PipeObject pipe in atmosNeighbours)
                {
                    if (activeDirection[j] == true)
                    {
                        difference = (atmosContainer.GetTemperature() - pipe.atmosContainer.GetTemperature()) * Gas.thermalBase * atmosContainer.Volume;

                        if (difference > Gas.thermalEpsilon)
                        {
                            pipe.atmosContainer.SetTemperature(pipe.atmosContainer.GetTemperature() + difference);
                            atmosContainer.SetTemperature(atmosContainer.GetTemperature() - difference);
                            tempSetting = true;
                        }
                    }
                    j++;
                }
            }
            else if (state == AtmosStates.Semiactive)
            {
                SimulateMixing();
            }
        }

        public void SimulateMixing()
        {
            float[] gasses = atmosContainer.GetGasses();

            if (AtmosHelper.ArrayZero(gasses, Gas.mixRate))
                return;

            bool mixed = false;
            Array.Clear(gasDifference, 0, gasDifference.Length);

            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe == null)
                    continue;
                if (pipe.state != AtmosStates.Blocked)
                {
                    // Get difference in total moles and individual gasses
                    float pressure = GetTotalMoles();
                    float neighbourPressure = pipe.GetTotalMoles();
                    ArrayDiff(gasDifference, gasses, pipe.atmosContainer.GetGasses(), Gas.mixRate);

                    // If our moles / mixrate > 0.1f
                    if (!AtmosHelper.ArrayZero(gasDifference, Gas.mixRate))
                    {
                        // Set neighbour gasses to the normalized 
                        ArraySum(pipe.atmosContainer.GetGasses(), gasDifference);
                        AtmosHelper.ArrayNorm(pipe.atmosContainer.GetGasses(), neighbourPressure);

                        if (pipe.state == AtmosStates.Inactive)
                        {
                            pipe.state = AtmosStates.Semiactive;
                        }

                        // Set our own gasses to the normalized
                        ArrayDiff(gasses, gasses, gasDifference, Gas.mixRate);
                        AtmosHelper.ArrayNorm(gasses, pressure);
                        mixed = true;
                    }
                }
            }

            if (!mixed && state == AtmosStates.Semiactive)
            {
                state = AtmosStates.Inactive;
            }
        }

        public void ArrayDiff(float[] difference, float[] arr1, float[] arr2, float mixRate)
        {
            for (int i = 0; i < Gas.numOfGases; ++i)
            {
                difference[i] = (arr1[i] - arr2[i]) * mixRate;
            }
        }

        public void ArraySum(float[] arr1, float[] arr2)
        {
            for (int i = 0; i < Gas.numOfGases; ++i)
            {
                arr1[i] = arr1[i] + arr2[i];
            }
        }
    }
}