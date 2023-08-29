using Coimbra;
using SS3D.Substances;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Represent a generic body part for humans, without any particular mechanisms.
/// </summary>
public class HumanBodypart : BodyPart
{
    public override void Init(BodyPart parent)
    {
        base.Init(parent);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this));
        InvokeOnBodyPartLayerAdded();
    }
}
