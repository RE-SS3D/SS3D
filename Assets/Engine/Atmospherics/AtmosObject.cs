using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
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

        private bool[] activeDirection = {
                false,  // Top AtmosObject active
                false,  // Bottom AtmosObject active
                false,  // Left AtmosObject active
                false   // Right AtmosObject active
            };

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

        public void CalculateFlux()
        {
            float[] otherTilesFlux = {
                0f,     // Top flux
                0f,     // Bottom flux
                0f,     // Left flux
                0f      // Bottom flux
            };

            if (state == AtmosStates.Active)
            {

                int i = 0;
                foreach (AtmosObject tile in atmosNeighbours)
                {
                    if (tile != null && tile.state != AtmosStates.Blocked)
                    {
                        otherTilesFlux[i] = Mathf.Min(tileFlux[i] * drag + (GetPressure() - tile.GetPressure()) * dt, 1000f);
                        activeDirection[i] = true;

                        if (otherTilesFlux[i] < 0f)
                        {
                            tile.state = AtmosStates.Active;
                            otherTilesFlux[i] = 0f;
                        }
                    }
                    i++;
                }

                if (otherTilesFlux[0] > fluxEpsilon || otherTilesFlux[1] > fluxEpsilon || otherTilesFlux[2] > fluxEpsilon || otherTilesFlux[3] > fluxEpsilon)
                {
                    float scalingFactor = Mathf.Min(1, GetPressure() / (otherTilesFlux[0] + otherTilesFlux[1] + otherTilesFlux[2] + otherTilesFlux[3]) / dt);

                    for (int j = 0; j < 4; j++)
                    {
                        otherTilesFlux[j] *= scalingFactor;
                        tileFlux[j] = otherTilesFlux[j];
                    }
                }
                else
                {
                    for (int j = 0; j < 4; j++)
                    {
                        tileFlux[j] = 0;
                    }
                    if (!tempSetting) { state = AtmosStates.Semiactive; } else { tempSetting = false; }
                }
            }
			if (state == AtmosStates.Active || state == AtmosStates.Semiactive)
			{
                
                // SimulateMixing();
			}
        }

        public void SimulateFlux()
        {
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
                float velVertical = tileFlux[0] - tileFlux[1];    // Top - bottom flux
                float velHorizontal = tileFlux[2] - tileFlux[3];      // Left - right flux

                velocity = new Vector2(velHorizontal, velVertical);
            }
            else if (state == AtmosStates.Semiactive)
            {
                SimulateMixing();
            }
        }

        public void SimulateMixing()
        {
            if (AtmosHelper.ArrayZero(gasses, mixRate))
                return;

            bool mixed = false;
            float[] difference = new float[AtmosManager.numOfGases];

            // Check the mole difference between each gas
            for (int i = 0; i < AtmosManager.numOfGases; i++)
            {
                // There must be gas of course...
                if (gasses[i] > 0f)
                {
                    // Go through all neighbours
                    foreach (AtmosObject atmosObject in atmosNeighbours)
                    {
                        if (atmosObject == null)
                            continue;
                        if (atmosObject.state != AtmosStates.Blocked)
                        {
                            difference[i] = (gasses[i] - atmosObject.gasses[i]) * 0.2f;
                            if (difference[i] > 0.1f)
                            {
                                // Increase neighbouring tiles moles
                                atmosObject.gasses[i] += difference[i];
                                atmosObject.state = AtmosStates.Semiactive;

                                // Decrease our own moles
                                gasses[i] -= difference[i];
                                mixed = true;
                            }
                        }
                    }
                }
            }

            foreach (AtmosObject atmosObject in atmosNeighbours)
            {
                if (atmosObject == null)
                    continue;
                if (atmosObject.state != AtmosStates.Blocked)
                {
                    // Get difference in total moles and individual gasses
                    float pressure = GetTotalMoles();
                    float neighbourPressure = atmosObject.GetTotalMoles();
                    difference = AtmosHelper.ArrayDiff(gasses, atmosObject.gasses, mixRate);

                    // If our moles / mixrate > 0.1f
                    if (AtmosHelper.ArrayZero(difference, mixRate))
                    {
                        // Set neighbour gasses to the normalized 
                        atmosObject.gasses = AtmosHelper.ArrayNorm(AtmosHelper.ArraySum(atmosObject.gasses, difference), neighbourPressure);

                        if (atmosObject.state == AtmosStates.Inactive)
                        {
                            atmosObject.state = AtmosStates.Semiactive;
                        }

                        // Set our own gasses to the normalized
                        gasses = AtmosHelper.ArrayNorm(AtmosHelper.ArrayDiff(gasses, difference, mixRate), pressure);
                        mixed = true;
                    }
                }
            }

            if (!mixed && state == AtmosStates.Semiactive)
            {
                state = AtmosStates.Inactive;
            }
        }
    }
}