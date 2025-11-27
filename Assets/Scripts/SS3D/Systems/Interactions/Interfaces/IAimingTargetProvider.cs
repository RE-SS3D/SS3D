using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAimingTargetProvider
{ 
    public Transform AimTarget { get; }

    public bool IsAimingToThrow { get; }
}
