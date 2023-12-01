using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

public interface IPowerStorage : IElectricDevice
{
    public float StoredPower { get; set; }

    public float MaxCapacity { get; }

    public float RemainingCapacity { get; }
}
