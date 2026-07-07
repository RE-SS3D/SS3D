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

        // Fraction of an edge's temperature gap equalized by conduction per second. Kept well
        // below the 4-neighbour overshoot limit so the Jacobi gather stays monotone.
        public const float HeatConductionRate = 0.6f;
        public const float BreachPressureThreshold = 50f;
        public const int MaxBreachSubsteps = 4;

        // --- Combustion (plasma fire) ---
        // Plasma only burns at or above this temperature; released heat keeps it above the
        // threshold so the fire self-sustains until a reactant runs out.
        public const float PlasmaIgnitionTemperature = 373.15f;

        // Base fraction of available plasma consumed per second, scaled up with temperature.
        public const float PlasmaBurnRate = 1.0f;

        // Moles of oxygen consumed per mole of plasma burned.
        public const float OxygenPerPlasma = 2.0f;

        // Thermal energy released per mole of plasma burned (tuning units, matches specific heats).
        public const float FireEnergyPerMole = 500000f;

        // Ignore reactant amounts below this to avoid denormal churn.
        public const float MinimumBurnMoles = 0.01f;
    }
}
