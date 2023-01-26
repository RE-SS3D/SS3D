using Codice.Client.Commands;
using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class HumanBodypart : BodyPart
{
    public bool IsConnectedToCentralNervousSystem { get; set; }

    public HumanBodypart() : base() 
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));

        var nerverLayer = new NerveLayer(this);
        BodyLayers.Add(new NerveLayer(this));
    }

    public HumanBodypart(BodyPartBehaviour bodyPartBehaviour) : base(bodyPartBehaviour)
    {
        BodyLayers.Add(new MuscleLayer(this));
        BodyLayers.Add(new BoneLayer(this));
        BodyLayers.Add(new CirculatoryLayer(this));

        var nerverLayer = new NerveLayer(this);
        BodyLayers.Add(new NerveLayer(this));
    }

    private void OnNerveDamaged(object sender, DamageEventArgs nerveDamageEventArgs)
    {

    }
   
}
