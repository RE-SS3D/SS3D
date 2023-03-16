using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;

/// <summary>
/// TODO : Make an organ bodylayer since that might be useful as all organs will have the same damage susceptibility.
/// When the brain dies, the player dies.
/// </summary>
public class Brain : BodyPart
{


    public Brain(bool hasCentralNervousSystem)
    {
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, hasCentralNervousSystem)); 
        BodyLayers.Add(new OrganLayer(this));
    }

    public Brain(BodyPartBehaviour bodyPartBehaviour, bool hasCentralNervousSystem) : base(bodyPartBehaviour)
    {
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, hasCentralNervousSystem));
        BodyLayers.Add(new OrganLayer(this));
    }

    /// <summary>
    /// This takes all the pain in the body and sum it.
    /// </summary>
    public float ProcessPain(BodyPart[] bodyParts)
    {
        float painSum = 0;
        foreach (var bodyPart in bodyParts)
        {
            var transmitters = bodyPart.BodyLayers.FindAll(x => x is INerveSignalTransmitter);
            foreach (var transmitter in transmitters)
            {
                var t = (INerveSignalTransmitter)transmitter;
                if (t.IsConnectedToCentralNervousSystem)
                {
                    painSum += t.ProducePain();
                }
            }
        }
        return painSum;
    }
}
