using System;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    /*
     * Ideal Gas Law
     * PV = nRT
     * 
     * P - Measured in pascals, 101.3 kPa
     * V - Measured in cubic meters, 1 m^3
     * n - Moles
     * R - Gas constant, 8.314
     * T - Measured in kelvin, 293 K
     * 
     * Human air consumption is 0.016 moles of oxygen per minute
     * 
     * Oxygen	        Needed for breathing, less than 16kPa causes suffocation
     * Carbon Dioxide   Causes suffocation at 8kPa
     * Plasma	        Ignites at high pressures in the presence of oxygen
     */

    public enum AtmosStates
    {
        Active,     // Tile is active; equalizes pressures, temperatures and mixes gasses
        Semiactive, // No pressure equalization, but mixes gasses
        Inactive,   // Do nothing
        Vacuum,     // Drain other tiles
        Blocked     // Wall, skips calculations
    }

    public enum AtmosGasses
    {
        Oxygen,
        Nitrogen,
        CarbonDioxide,
        Plasma
    }

    public class AtmosObject : ScriptableObject
    {
        private AtmosManager manager;

        // Gass stuff
        private float[] gasses = new float[AtmosManager.numOfGases];
        private float temperature = 293f;
        private float[] tileFlux = { 0f, 0f, 0f, 0f };
        private Vector2 velocity = Vector2.zero;

        private AtmosStates state = AtmosStates.Active;
        private TileObject[] tileNeighbours = { null, null, null, null };
        private AtmosObject[] atmosNeighbours = { null, null, null, null };
        private bool tempSetting = false;

        // Gass constants
        private const float dt = 0.1f;              // Delta time
        private const float gasConstant = 8.314f;   // Universal gas constant
        private const float volume = 2.5f;          // Volume of each tile in cubic meters
        private const float drag = 0.95f;           // Fluid drag, slows down flux so that gases don't infinitely slosh
        private const float thermalRate = 0.024f * volume;  // Rate of temperature equalization
        private const float mixRate = 0.02f;        // Rate of gas mixing
        private const float fluxEpsilon = 0.025f;   // Minimum pressure difference to simulate
        private const float thermalEpsilon = 0.01f;	// Minimum temperature difference to simulate

        // Performance makers
        static ProfilerMarker s_CalculateFluxPerfMarker = new ProfilerMarker("AtmosObject.CalculateFlux");
        static ProfilerMarker s_CalculateFluxOnePerfMarker = new ProfilerMarker("AtmosObject.CalculateFlux.One");
        static ProfilerMarker s_SimulateFluxPerfMarker = new ProfilerMarker("AtmosObject.SimulateFlux");
        static ProfilerMarker s_SimlateMixingPerfMarker = new ProfilerMarker("AtmosObject.SimulateMixing");

        // Array allocation
        private bool[] activeDirection = {
                false,  // Top AtmosObject active
                false,  // Bottom AtmosObject active
                false,  // Left AtmosObject active
                false   // Right AtmosObject active
            };

        private float[] gasDifference = new float[AtmosManager.numOfGases];
        private float[] neighbourFlux = new float[4];

        public void setTileNeighbour(TileObject neighbour, int index)
        {
            tileNeighbours[index] = neighbour;
        }

        public void setAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                    atmosNeighbours[i] = tile.atmos;
                i++;
            }
        }

        public AtmosStates GetState()
        {
            return state;
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

        public float[] GetGasses()
        {
            return gasses;
        }

        public float GetTemperature()
        {
            return temperature;
        }

        public Vector2 GetVelocity()
        {
            return velocity;
        }

        public void RemoveFlux()
        {
            tileFlux = new float[]{ 0f, 0f, 0f, 0f} ;
        }

        public void AddGas(AtmosGasses gas, float amount)
        {
            if (state != AtmosStates.Blocked)
            {
                gasses[(int)gas] = Mathf.Max(gasses[(int)gas] + amount, 0);
                state = AtmosStates.Active;
            }
        }

        public void SetGasses(float[] amounts)
        {
            for (int i = 0; i < Mathf.Min(amounts.GetLength(0), AtmosManager.numOfGases); ++i)
            {
                gasses[i] = Mathf.Max(amounts[i], 0);
            }

            state = AtmosStates.Active;
        }

        public void MakeEmpty()
        {

            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                gasses[i] = 0f;
            }
        }

        public void MakeAir()
        {
            MakeEmpty();

            gasses[(int)AtmosGasses.Oxygen] = 20.79f;
            gasses[(int)AtmosGasses.Nitrogen] = 83.17f;
            temperature = 293f;
        }

        public void AddHeat(float temp)
        {
            temperature += Mathf.Max(temp - temperature, 0f) / GetSpecificHeat() * (100 / GetTotalMoles()) * dt;
            state = AtmosStates.Active;
        }

        public void RemoveHeat(float temp)
        {
            temperature -= Mathf.Max(temp - temperature, 0f) / GetSpecificHeat() * (100 / GetTotalMoles()) * dt;
            if (temperature < 0f)
            {
                temperature = 0f;
            }
            state = AtmosStates.Active;
        }

        public float GetTotalMoles()
        {
            float moles = 0f;
            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                moles += gasses[i];
            }
            return moles;
        }

        public float GetPressure()
        {
            float pressure = 0f;
            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                pressure += (gasses[i] * gasConstant * temperature) / volume;
            }
            return pressure / 1000f;    // Convert to KiloPascals
        }

        public float GetPartialPressure(int index)
        {
            return (gasses[index] * gasConstant * temperature) / volume / 1000f;
        }

        public float GetPartialPressure(AtmosGasses gas)
        {
            return (gasses[(int)gas] * gasConstant * temperature) / volume / 1000f;
        }

        public bool IsBreathable()
        {
            return (GetPartialPressure(AtmosGasses.Oxygen) >= 16f && GetPartialPressure(AtmosGasses.CarbonDioxide) < 8f);
        }

        public void ValidateVacuum()
        {
            // If a tile has no neighbour and is not a wall, consider it vacuum
            bool emptyTileFound = false;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile == null)
                    emptyTileFound = true;
            }
            if (emptyTileFound && state != AtmosStates.Blocked)
            {
                state = AtmosStates.Vacuum;
                MakeEmpty();
            }
        }

        public float GetSpecificHeat()
        {
            float temp = 0f;
            temp += gasses[(int)AtmosGasses.Oxygen] * 2f;           // Oxygen, 20
            temp += gasses[(int)AtmosGasses.Nitrogen] * 20f;        // Nitrogen, 200
            temp += gasses[(int)AtmosGasses.CarbonDioxide] * 3f;    // Carbon Dioxide, 30
            temp += gasses[(int)AtmosGasses.Plasma] * 1f;           // Plasma, 10
            return temp / GetTotalMoles();
        }

        public float GetMass()
        {
            float mass = 0f;
            mass += gasses[(int)AtmosGasses.Oxygen] * 32f;          // Oxygen
            mass += gasses[(int)AtmosGasses.Nitrogen] * 28f;        // Nitrogen
            mass += gasses[(int)AtmosGasses.CarbonDioxide] * 44f;   // Carbon Dioxide
            mass += gasses[(int)AtmosGasses.Plasma] * 78f;          // Plasma
            return mass;     // Mass in grams
        }

        public bool IsBurnable()
        {
            // TODO determine minimum burn ratio
            return (gasses[(int)AtmosGasses.Oxygen] > 1f && gasses[(int)AtmosGasses.Plasma] > 1f);
        }

        public void CalculateFlux()
        {
            s_CalculateFluxPerfMarker.Begin();
            Array.Clear(neighbourFlux, 0, neighbourFlux.Length);
            s_CalculateFluxOnePerfMarker.Begin();
            int i = 0;
            float pressure = GetPressure();
            foreach (AtmosObject tile in atmosNeighbours)
            {
                if (tile != null && tile.state != AtmosStates.Blocked)
                {
                    neighbourFlux[i] = Mathf.Min(tileFlux[i] * drag + (pressure - tile.GetPressure()) * dt, 1000f);
                    activeDirection[i] = true;

                    if (neighbourFlux[i] < 0f)
                    {
                        tile.state = AtmosStates.Active;
                        neighbourFlux[i] = 0f;
                    }
                }

                i++;
            }

            s_CalculateFluxOnePerfMarker.End();

            if (neighbourFlux[0] > fluxEpsilon || neighbourFlux[1] > fluxEpsilon || neighbourFlux[2] > fluxEpsilon ||
                neighbourFlux[3] > fluxEpsilon)
            {
                float scalingFactor = Mathf.Min(1,
                    pressure / (neighbourFlux[0] + neighbourFlux[1] + neighbourFlux[2] + neighbourFlux[3]) / dt);

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

            s_CalculateFluxPerfMarker.End();
        }

        public void SimulateFlux()
        {
            s_SimulateFluxPerfMarker.Begin();
            if (state == AtmosStates.Active)
            {
                float pressure = GetPressure();

                for (int i = 0; i < AtmosManager.numOfGases; i++)
                {
                    if (gasses[i] < 1f)
                        gasses[i] = 0f;

                    if (gasses[i] > 0f)
                    {
                        int k = 0;
                        foreach (AtmosObject tile in atmosNeighbours)
                        {
                            if (tileFlux[k] > 0f)
                            {
                                float factor = gasses[i] * (tileFlux[k] / pressure);
                                if (tile.state != AtmosStates.Vacuum)
                                {
                                    tile.gasses[i] += factor;
                                    tile.state = AtmosStates.Active;
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
                foreach (AtmosObject tile in atmosNeighbours)
                {
                    if (activeDirection[j] == true)
                    {
                        difference = (temperature - tile.temperature) * thermalRate; // / (GetSpecificHeat() * 5f);

                        if (difference > thermalEpsilon)
                        {
                            tile.temperature += difference;
                            temperature -= difference;
                            tempSetting = true;
                        }
                    }
                    j++;
                }

                float fluxFromLeft = 0;
                if (atmosNeighbours[2] != null)
                    fluxFromLeft = atmosNeighbours[2].tileFlux[3];
                float fluxFromRight = 0;
                if (atmosNeighbours[3] != null)
                    fluxFromLeft = atmosNeighbours[3].tileFlux[2];

                float fluxFromTop = 0;
                if (atmosNeighbours[0] != null)
                    fluxFromTop = atmosNeighbours[0].tileFlux[1];

                float fluxFromBottom = 0;
                if (atmosNeighbours[1] != null)
                    fluxFromBottom = atmosNeighbours[1].tileFlux[0];

                float velHorizontal = tileFlux[3] - fluxFromLeft - tileFlux[2] + fluxFromRight;
                float velVertical = tileFlux[0] - fluxFromTop - tileFlux[1] + fluxFromBottom;

                velocity = new Vector2(velHorizontal, velVertical);
            }
            else if (state == AtmosStates.Semiactive)
            {
                velocity = Vector2.zero;
                SimulateMixing();
            }
            s_SimulateFluxPerfMarker.End();
        }

        public void SimulateMixing()
        {
            if (AtmosHelper.ArrayZero(gasses, mixRate))
                return;

            s_SimlateMixingPerfMarker.Begin();
            bool mixed = false;
            Array.Clear(gasDifference, 0, gasDifference.Length);

            foreach (AtmosObject atmosObject in atmosNeighbours)
            {
                if (atmosObject == null)
                    continue;
                if (atmosObject.state != AtmosStates.Blocked)
                {
                    // Get difference in total moles and individual gasses
                    float pressure = GetTotalMoles();
                    float neighbourPressure = atmosObject.GetTotalMoles();
                    ArrayDiff(gasDifference, gasses, atmosObject.gasses, mixRate);

                    // If our moles / mixrate > 0.1f
                    if (AtmosHelper.ArrayZero(gasDifference, mixRate))
                    {
                        // Set neighbour gasses to the normalized 
                        ArraySum(atmosObject.gasses, gasDifference);
                        AtmosHelper.ArrayNorm(atmosObject.gasses, neighbourPressure);

                        if (atmosObject.state == AtmosStates.Inactive)
                        {
                            atmosObject.state = AtmosStates.Semiactive;
                        }

                        // Set our own gasses to the normalized
                        ArrayDiff(gasses, gasses, gasDifference, mixRate);
                        AtmosHelper.ArrayNorm(gasses, pressure);
                        mixed = true;
                    }
                }
            }

            if (!mixed && state == AtmosStates.Semiactive)
            {
                state = AtmosStates.Inactive;
            }
            s_SimlateMixingPerfMarker.End();
        }

        public void ArrayDiff(float[] difference, float[] arr1, float[] arr2, float mixRate)
        {
            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                difference[i] = (arr1[i] - arr2[i]) * mixRate;
            }
        }

        public void ArraySum(float[] arr1, float[] arr2)
        {
            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                arr1[i] = arr1[i] + arr2[i];
            }
        }
    }
}