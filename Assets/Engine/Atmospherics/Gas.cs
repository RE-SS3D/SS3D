using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

namespace SS3D.Engine.Atmospherics
{

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

    public static class Gas
    {
        // Gass constants
        public const float dt = 0.1f;               // Delta time
        public const float gasConstant = 8.314f;    // Universal gas constant
        public const float drag = 0.95f;            // Fluid drag, slows down flux so that gases don't infinitely slosh
        public const float thermalBase = 0.024f;    // * volume | Rate of temperature equalization
        public const float mixRate = 0.05f;        // Rate of gas mixing
        public const float fluxEpsilon = 0.025f;    // Minimum pressure difference to simulate
        public const float thermalEpsilon = 0.01f;  // Minimum temperature difference to simulate

        public const float windFactor = 0.2f;       // How much force will any wind apply
        public const float minimumWind = 1f;        // Minimum wind required to move items

        public static int numOfGases = Enum.GetNames(typeof(AtmosStates)).Length;
    }
}