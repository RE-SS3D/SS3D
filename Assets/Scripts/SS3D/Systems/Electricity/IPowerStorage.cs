namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Interface for devices that store electrical energy and feed it back into the grid.
    /// Stored values are kWh; charge and discharge rates are kW per tick.
    /// </summary>
    public interface IPowerStorage : IElectricDevice
    {
        /// <summary>
        /// Energy currently stored in kWh.
        /// </summary>
        float StoredEnergyKwh { get; }

        /// <summary>
        /// Maximum storable energy in kWh.
        /// </summary>
        float MaxCapacityKwh { get; }

        /// <summary>
        /// Remaining storage headroom in kWh.
        /// </summary>
        float RemainingCapacityKwh { get; }

        /// <summary>
        /// Maximum discharge rate in kW per tick.
        /// </summary>
        float MaxDischargeRateKw { get; }

        /// <summary>
        /// Maximum charge rate in kW per tick.
        /// </summary>
        float MaxChargeRateKw { get; }

        /// <summary>
        /// Maximum deliverable power this tick in kW.
        /// </summary>
        float MaxDeliverableKw(float tickSeconds);

        /// <summary>
        /// Whether the storage can send or receive power.
        /// </summary>
        bool IsOn { get; }

        /// <summary>
        /// Discharge up to the requested kW this tick, respecting rate and stored energy.
        /// </summary>
        /// <returns>kW actually delivered this tick.</returns>
        float RemovePowerKw(float requestedKw, float tickSeconds);

        /// <summary>
        /// Charge up to the requested kW this tick, respecting rate and remaining capacity.
        /// </summary>
        /// <returns>kW actually absorbed this tick.</returns>
        float AddPowerKw(float requestedKw, float tickSeconds);
    }
}
