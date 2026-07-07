namespace SS3D.Systems.Atmospherics.ECS
{
    public static class AtmosFluxConstants
    {
        public const float GasConstant = 8.314f;
        public const float SimSpeed = 1.0f;

        // Cells venting into vacuum have no neighbour to overfill, so there is no stability
        // limit: drain them far faster than normal diffusion so a breach empties quickly.
        public const float VacuumVentSpeed = 5.0f;

        // Hard cap on the fraction of a cell's gas that can vent through a single vacuum edge in one
        // tick. Prevents a cell from fully emptying in a single step (which collapses its heat
        // capacity to ~0 and makes its temperature oscillate); the residual drains next tick.
        public const float MaxVentFraction = 0.5f;
        public const float PressureEpsilon = 1.0f;
        public const float FluxEpsilon = 0.05f;
        public const float ThermalBase = 0.024f;
        public const float ThermalEpsilon = 0.5f;

        // Fraction of an edge's temperature gap equalized by conduction per second. Kept well
        // below the 4-neighbour overshoot limit so the Jacobi gather stays monotone.
        public const float HeatConductionRate = 0.6f;

        // Fraction of the gap to space temperature a vacuum-exposed cell relaxes per second.
        // Vacuum is a heat sink just as it is a pressure sink; energy leaves and is not deposited
        // anywhere. Applied as a direct temperature relaxation so it keeps cooling even a cell
        // that has vented to near-vacuum (where an energy-based update would divide by ~0).
        public const float SpaceConductionRate = 1.0f;
        public const float BreachPressureThreshold = 50f;
        public const int MaxBreachSubsteps = 4;

        // --- Near-vacuum temperature stabilization ---
        // Below this many moles a cell is treated as effectively empty and its temperature is
        // blended toward space temperature. This prevents the E / heatCapacity division from
        // exploding (and oscillating) when a vented cell's heat capacity approaches zero.
        public const float VacuumFadeMoles = 2.0f;

        // Virtual heat capacity of the space-temperature reservoir mixed into a fully empty cell.
        // Large enough to dominate a near-empty cell's own capacity so its temperature resolves
        // stably to space temperature; fades to zero for cells with meaningful gas.
        public const float VacuumHeatCapacityFloor = 100f;

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
