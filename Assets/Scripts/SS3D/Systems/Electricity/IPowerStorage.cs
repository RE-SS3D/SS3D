using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

/// <summary>
/// Interface for things that can receive, store power and send it back.
/// </summary>
public interface IPowerStorage : IElectricDevice
{
    /// <summary>
    /// How much power is currently stored.
    /// </summary>
    public float StoredPower { get; }

    /// <summary>
    /// How much power can this store.
    /// </summary>
    public float MaxCapacity { get; }

    /// <summary>
    /// How much power storage room is left.
    /// </summary>
    public float RemainingCapacity { get; }

    /// <summary>
    /// How much power one can remove in one update, considering there's enough power left.
    /// </summary>
    public float MaxPowerRate { get; }

    /// <summary>
    /// How much power one can remove in one update.
    /// </summary>
    public float MaxRemovablePower { get; }

    /// <summary>
    /// If the storage can send or receive power.
    /// </summary>
    public bool IsOn { get; }

    /// <summary>
    ///  Remove a given amount of power from battery, respecting Max power rate and present amount.*
    ///  Should also not remove power if the storage is off.
    /// </summary>
    /// <returns> Return the power amount removed. </returns>
    public float RemovePower(float amount);

    /// <summary>
    /// Add power to battery, respecting maximum.
    /// Should also not add power if the storage is off.
    /// </summary>
    /// <returns> Return the power amount added. </returns>
    public float AddPower(float amount);
}
