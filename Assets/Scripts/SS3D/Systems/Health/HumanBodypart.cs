using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Represent a generic body part, containing muscles, bones, nerves and veins.
/// Can be used for arms, foot, legs ... Should not be used for organs.
/// </summary>
public class HumanBodypart : BodyPart
{

    public HumanBodypart(string name = "") : base(name) 
    {
        AddHumanBodyLayers();
    }

    public HumanBodypart(BodyPartBehaviour bodyPartBehaviour, string name = "") : base(bodyPartBehaviour, name)
    {
        AddHumanBodyLayers();
    }

    public HumanBodypart(BodyPart parent,string name = "") : base(parent, name)
    {
        AddHumanBodyLayers();
    }

    private void OnNerveDamaged(object sender, DamageEventArgs nerveDamageEventArgs)
    {

    }

    private void AddHumanBodyLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this, false));
    }
   
}
