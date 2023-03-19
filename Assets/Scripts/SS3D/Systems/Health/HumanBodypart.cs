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
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, false));
    }

    public HumanBodypart(BodyPartBehaviour bodyPartBehaviour, string name = "") : base(bodyPartBehaviour, name)
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, false));
    }

    public HumanBodypart(BodyPart parent,string name = "") : base(parent, name)
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, false));
    }

    private void OnNerveDamaged(object sender, DamageEventArgs nerveDamageEventArgs)
    {

    }
   
}
