namespace SS3D.Systems.Atmospherics.ECS
{
    public static class AtmosFluxConstants
    {
        public const float GasConstant = 8.314f;
        public const float PressureEpsilon = 1.0f;

        // Equal-pressure composition mixing: when total pressures match within PressureEpsilon,
        // still exchange moles along partial-pressure gradients so breath / scrubber pockets
        // dilute into the room. Stays slow so stable rooms do not thrash.
        public const float DiffusionSpeed = 0.15f;

        // Ignore tiny partial-pressure gaps so a nearly-uniform room can sleep.
        public const float PartialPressureEpsilon = 0.05f;

        // A nearly-evacuated cell that can still drain (touches vacuum, or a neighbour that is
        // itself this empty) dumps its last traces and sleeps once it falls below this pressure.
        // Venting is proportional to the pressure gap, so the approach to true vacuum is otherwise
        // an ever-slowing exponential tail that keeps cells awake indefinitely; this lets a breached
        // room actually settle to zero instead of creeping toward it forever.
        public const float MinSimulationPressure = 5.0f;
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
        public const int MaxBreachSubsteps = 4;

        // --- Edge flow (bulk transport with momentum, Documents/architecture/2026-11_atmos-edge-flow.md) ---
        // How fast a pressure wave crosses open tiles at EdgeFlowReferenceTemperature, tiles/s.
        // Wave speed rises with the square root of temperature, as sound does.
        public const float EdgeFlowWaveSpeed = 12f;
        public const float EdgeFlowReferenceTemperature = 293.15f;

        // Upper bound on (wave speed × dt)² for the explicit 2D update. An edge that would exceed
        // it, because it is hot or runs on a long calm step, is stiffened no further.
        public const float EdgeFlowStabilityLimit = 0.49f;

        // Friction per second. Above 2π·c/L the longest mode of a room of L tiles stops ringing.
        public const float EdgeFlowFriction = 8f;

        // Fastest gas may cross an open edge, tiles/s. Flow is capped at this speed × upwind moles.
        public const float EdgeFlowMaxSpeed = 30f;

        // The same cap on an edge into vacuum; it stands in for choked flow.
        public const float EdgeFlowVacuumMaxSpeed = 6f;

        // Fraction of a cell's gas that may leave it in one substep, across all its edges. A cell
        // that fully empties has ~zero heat capacity, and its temperature loses any stable basis.
        public const float EdgeFlowMaxOutflowFraction = 0.5f;

        // An edge below both of these has settled: its flow is zeroed so both cells can sleep.
        public const float EdgeFlowSettledPressure = 0.1f;
        public const float EdgeFlowSettledFlow = 1f;

        // Above either of these anywhere in the active set, the tick runs MaxBreachSubsteps.
        public const float EdgeFlowSubstepPressure = 0.5f;
        public const float EdgeFlowSubstepFlow = 20f;

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
        public const float PlasmaBurnRate = 0.5f;

        // Moles of oxygen consumed per mole of plasma burned.
        public const float OxygenPerPlasma = 2.0f;

        // Thermal energy released per mole of plasma burned (tuning units, matches specific heats).
        public const float FireEnergyPerMole = 500000f;

        // Ignore reactant amounts below this to avoid denormal churn.
        public const float MinimumBurnMoles = 0.01f;
    }
}
