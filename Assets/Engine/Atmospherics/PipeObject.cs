﻿using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class PipeObject : MonoBehaviour
    {
        private const float maxPipePressure = 2000f;

        public float volume = 2.5f;

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

        private float[] difference = new float[Gas.numOfGases];
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

        public float GetTemperature()
        {
            return atmosContainer.GetTemperature();
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
                float scalingFactor = Mathf.Min(1f,
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

            if (state == AtmosStates.Semiactive || state == AtmosStates.Active)
            {
                SimulateMixing();
            }
        }

        public void SimulateFlux()
        {
            float[] gasses = atmosContainer.GetGasses();

            float plasmaIn = gasses[3];
            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe)
                    plasmaIn += pipe.GetAtmosContainer().GetGasses()[3];
            }


            if (state == AtmosStates.Active)
            {
                float pressure = GetPressure();

                for (int i = 0; i < Gas.numOfGases; i++)
                {
                    //if (gasses[i] < 1f)
                    //    gasses[i] = 0f;

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

            float plasmaOut = gasses[3];
            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe)
                    plasmaOut += pipe.GetAtmosContainer().GetGasses()[3];
            }

            if (Mathf.Abs(plasmaIn - plasmaOut) > 0.01f)
                Debug.Log("Plasma leak in SimulateFlux()! Going in: " + plasmaIn + " Going out: " + plasmaOut);
        }

        public void SimulateMixing()
        {
            float[] gasses = atmosContainer.GetGasses();

            float plasmaIn = gasses[3];
            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe)
                    plasmaIn += pipe.GetAtmosContainer().GetGasses()[3];
            }
            //if (plasmaIn > 0.1)
            //    Debug.Log("Plasma in: " + plasmaIn);


            if (AtmosHelper.ArrayZero(gasses, Gas.mixRate))
                return;

            bool mixed = false;
            Array.Clear(difference, 0, difference.Length);


            for (int i = 0; i < Gas.numOfGases; i++)
            {
                // There must be gas of course...
                if (gasses[i] > 0f)
                {
                    // Go through all neighbours
                    foreach (PipeObject atmosObject in atmosNeighbours)
                    {
                        if (atmosObject == null)
                            continue;
                        if (atmosObject.state != AtmosStates.Blocked)
                        {
                            difference[i] = (gasses[i] - atmosObject.GetAtmosContainer().GetGasses()[i]) * Gas.mixRate;
                            if (difference[i] > 0.01f)
                            {
                                // Increase neighbouring tiles moles
                                atmosObject.GetAtmosContainer().GetGasses()[i] += difference[i];
                                if (Mathf.Abs(atmosObject.GetPressure() - GetPressure()) > 0.1f)
                                {
                                    atmosObject.state = AtmosStates.Active;
                                }
                                else
                                {
                                    atmosObject.state = AtmosStates.Semiactive;
                                }
                                

                                // Decrease our own moles
                                gasses[i] -= difference[i];
                                if (gasses[i] <= 0f && difference[i] > 0.01f)
                                    Debug.LogError("Zero gas plasma set");

                                mixed = true;
                            }
                        }
                    }
                }
            }

            float plasmaOut = gasses[3];
            foreach (PipeObject pipe in atmosNeighbours)
            {
                if (pipe)
                    plasmaOut += pipe.GetAtmosContainer().GetGasses()[3];
            }

            if (Mathf.Abs(plasmaIn - plasmaOut) > 0.01f)
                Debug.Log("Plasma leak! Going in: " + plasmaIn + " Going out: " + plasmaOut);

            //if (plasmaIn > 0.1)
            //    Debug.Log("Plasma out: " + plasmaOut);

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