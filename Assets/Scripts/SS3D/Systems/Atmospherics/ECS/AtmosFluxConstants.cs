namespace SS3D.Systems.Atmospherics.ECS
{
    public static class AtmosFluxConstants
    {
        public const float GasConstant = 8.314f;
        public const float SimSpeed = 0.2f;
        public const float PressureEpsilon = 1.0f;
        public const float FluxEpsilon = 0.05f;
        public const float ThermalBase = 0.024f;
        public const float ThermalEpsilon = 0.5f;
        public const float BreachPressureThreshold = 50f;
        public const int MaxBreachSubsteps = 4;
    }
}
