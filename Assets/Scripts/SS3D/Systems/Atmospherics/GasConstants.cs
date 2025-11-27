using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    public static class GasConstants
    {
        public const bool UseRealisticGasLaw = true;

        // Gas constants
        public const float SimSpeed = 0.2f;         // Simulation speed

        public const float GasConstant = 8.314f;    // Universal gas constant

        public const float Drag = 0.8f;         // Fluid drag, slows down flux so that gases don't infinitely slosh

        public const float ThermalBase = 0.024f;    // * volume | Rate of temperature equalization

        public const float DiffusionEpsilon = 0.1f;    // Minimum difference in moles to simulate

        public const float PressureEpsilon = 1.0f;

        public const float ThermalEpsilon = 0.5f;   // Minimum temperature difference to simulate

        public const float WindFactor = 0.1f;       // How much force will any wind apply

        public const float ActiveFluxFactor = 5f;   // help speeding up diffusion (if above 1) when transferring moles based on pressure difference.

        public static readonly float4 CoreSpecificHeat = new(
            2f, // Oxygen
            20f, // Nitrogen
            3f, // Carbon Dioxide
            10f); // Plasma

        public static readonly float4 CoreGasDensity = new(
            32f, // Oxygen
            28f, // Nitrogen
            44f, // Carbon Dioxide
            78f); // Plasma

        public static readonly float4 InterMolecularInteraction = new(
            1.382f, // Oxygen
            1.370f, // Nitrogen
            3.658f, // Carbon Dioxide
            5.353f /* Plasma */);

        /// <summary>
        /// Diffusion rate is dependant on molecular density. Heavier gasses diffuse slower.
        /// </summary>
        public static readonly float4 GasDiffusionRate = 4f / CoreGasDensity;
    }
}
