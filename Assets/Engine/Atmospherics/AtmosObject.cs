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
        Vacuum,
        Blocked
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

        private AtmosStates state = AtmosStates.Active;
        private TileObject[] tileNeighbours = { null, null, null, null };
        private AtmosObject[] atmosNeighbours = { null, null, null, null };

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

        public void RemoveFlux()
        {
            tileFlux = new float[]{ 0f, 0f, 0f, 0f} ;
        }

        public void AddGas(AtmosGasses gas, float amount)
        {
            gasses[(int)gas] = Mathf.Max(gasses[(int)gas] + amount, 0);
            state = AtmosStates.Active;
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
            temperature -= Mathf.Max(temperature - temp, 0f) / GetSpecificHeat() * (100 / GetTotalMoles()) * dt;
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
                }

                i++;

            }

			if (state == AtmosStates.Active || state == AtmosStates.Semiactive)
			{
				SimulateMixing();
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
            return; 
            bool mixed = false;
            float[] difference = new float[AtmosManager.numOfGases];

            /*for (int i = 0; i < _numOfGases; ++i)
			{
				if (gases[i] > 0f)
				{
					if (y < grid.GetLength(1) - 1 && grid[x, y + 1].state != States.blocked)
					{
						difference = (gases[i] - grid[x, y + 1].gases[i]) * 0.2f;

						if (difference > 0.1f)
						{
							grid[x, y + 1].gases[i] += difference;
							grid[x, y + 1].state = States.semiactive;
							gases[i] -= difference;
							mixed = true;
						}
					}
					/*if (y > 0 && grid[x, y - 1].state != States.blocked)
					{
						difference = (gases[i] - grid[x, y - 1].gases[i]) * 0.2f;

						if (difference > 0.1f)
						{
							grid[x, y - 1].gases[i] += difference;
							grid[x, y - 1].state = States.semiactive;
							gases[i] -= difference;
							mixed = true;
						}
					}
					if (x > 0 && grid[x - 1, y].state != States.blocked)
					{
						difference = (gases[i] - grid[x - 1, y].gases[i]) * 0.2f;

						if (difference > 0.1f)
						{
							grid[x - 1, y].gases[i] += difference;
							grid[x - 1, y].state = States.semiactive;
							gases[i] -= difference;
							mixed = true;
						}
					}
					if (x < grid.GetLength(0) - 1 && grid[x + 1, y].state != States.blocked)
					{
						difference = (gases[i] - grid[x + 1, y].gases[i]) * 0.2f;

						if (difference > 0.1f)
						{
							grid[x + 1, y].gases[i] += difference;
							grid[x, y + 1].state = States.semiactive;
							gases[i] -= difference;
							mixed = true;
						}
					}*//*
				}
			}*/

            /*if (y < grid.GetLength(1) - 1 && grid[x, y + 1].state != TileStates.Blocked)
			{
				difference = ArrayDiff(gasses, grid[x, y + 1].gasses);
				float pressure = GetMoles();
				float pressureOther = grid[x, y + 1].GetMoles();

				if (!ArrayZero(difference))
				{
					grid[x, y + 1].gasses = ArrayNorm(ArraySum(grid[x, y + 1].gasses, difference), pressureOther);
					if (grid[x, y + 1].state == TileStates.Inactive) { grid[x, y + 1].state = TileStates.Semiactive; }
					gasses = ArrayNorm(ArrayDiff(gasses, difference), pressure);
					mixed = true;
				}
			}
			if (y > 0 && grid[x, y - 1].state != TileStates.Blocked)
			{
				difference = ArrayDiff(gasses, grid[x, y - 1].gasses);
				float pressure = GetMoles();
				float pressureOther = grid[x, y - 1].GetMoles();

				if (!ArrayZero(difference))
				{
					grid[x, y - 1].gasses = ArrayNorm(ArraySum(grid[x, y - 1].gasses, difference), pressureOther);
					if (grid[x, y - 1].state == TileStates.Inactive) { grid[x, y - 1].state = TileStates.Semiactive; }
					gasses = ArrayNorm(ArrayDiff(gasses, difference), pressure);
					mixed = true;
				}
			}
			if (x > 0 && grid[x - 1, y].state != TileStates.Blocked)
			{
				difference = ArrayDiff(gasses, grid[x - 1, y].gasses);
				float pressure = GetMoles();
				float pressureOther = grid[x - 1, y].GetMoles();

				if (!ArrayZero(difference))
				{
					grid[x - 1, y].gasses = ArrayNorm(ArraySum(grid[x - 1, y].gasses, difference), pressureOther);
					if (grid[x - 1, y].state == TileStates.Inactive) { grid[x - 1, y].state = TileStates.Semiactive; }
					gasses = ArrayNorm(ArrayDiff(gasses, difference), pressure);
					mixed = true;
				}
			}
			if (x < grid.GetLength(0) - 1 && grid[x + 1, y].state != TileStates.Blocked)
			{
				difference = ArrayDiff(gasses, grid[x + 1, y].gasses);
				float pressure = GetMoles();
				float pressureOther = grid[x + 1, y].GetMoles();

				if (!ArrayZero(difference))
				{
					grid[x + 1, y].gasses = ArrayNorm(ArraySum(grid[x + 1, y].gasses, difference), pressureOther);
					if (grid[x + 1, y].state == TileStates.Inactive) { grid[x + 1, y].state = TileStates.Semiactive; }
					gasses = ArrayNorm(ArrayDiff(gasses, difference), pressure);
					mixed = true;
				}
			}

			if (!mixed && state == TileStates.Semiactive)
			{
				state = TileStates.Inactive;
			}*/
        }
    }
}