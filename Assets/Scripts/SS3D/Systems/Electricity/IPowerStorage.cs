using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

public interface IPowerStorage : IElectricDevice
{
    public float StoredPower { get; }

    public float MaxCapacity { get; }

    public float RemainingCapacity { get; }

    public float MaxPowerRate { get; }

    public float MaxRemovablePower { get; }

    /// <summary>
    ///  Remove a given amount of power from battery, respecting Max power rate and present amount.
    /// </summary>
    /// <returns> Return the power amount removed. </returns>
    public float RemovePower(float amount);

    /// <summary>
    /// Add power to battery, respecting maximum.
    /// </summary>
    /// <returns> Return the power amount added. </returns>
    public float AddPower(float amount);
}
