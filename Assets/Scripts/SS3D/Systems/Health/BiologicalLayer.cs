using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Biological layers take damage if not enough oxygen is provided.
/// </summary>
public abstract class BiologicalLayer : BodyLayer
{
    public abstract float OxygenConsumptionRate { get; }

    public BiologicalLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }
    /// <summary>
    /// Get oxygen from CirculatoryLayer if there's one. 
    /// If not enough oxygen is present, inflict OXY type damage to the layer.
    /// </summary>
    public virtual void ConsumeOxygen()
    {
        throw new NotImplementedException();
    }
}
