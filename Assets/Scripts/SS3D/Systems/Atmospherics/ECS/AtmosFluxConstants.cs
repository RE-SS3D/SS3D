namespace SS3D.Systems.Atmospherics.ECS
{
    public static class AtmosFluxConstants
    {
        public const float GasConstant = 8.314f;
        public const float SimSpeed = 1.0f;

        // Cells venting into vacuum have no neighbour to overfill, so there is no stability
        // limit: drain them far faster than normal diffusion so a breach empties quickly.
        public const float VacuumVentSpeed = 5.0f;
        public const float PressureEpsilon = 1.0f;
        public const float FluxEpsilon = 0.05f;
        public const float ThermalBase = 0.024f;
        public const float ThermalEpsilon = 0.5f;
        public const float BreachPressureThreshold = 50f;
        public const int MaxBreachSubsteps = 4;
    }
}
