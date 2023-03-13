using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class HumanBodypart : BodyPart
{

    public HumanBodypart(bool hasCentralNervousSystem) : base() 
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, hasCentralNervousSystem));
    }

    public HumanBodypart(BodyPartBehaviour bodyPartBehaviour, bool hasCentralNervousSystem) : base(bodyPartBehaviour)
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, hasCentralNervousSystem));
    }

    private void OnNerveDamaged(object sender, DamageEventArgs nerveDamageEventArgs)
    {

    }
   
}
