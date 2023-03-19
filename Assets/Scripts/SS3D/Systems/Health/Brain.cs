using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;
using Codice.Client.Common;

/// <summary>
/// TODO : Make an organ bodylayer since that might be useful as all organs will have the same damage susceptibility.
/// When the brain dies, the player dies.
/// </summary>
public class Brain : BodyPart
{


    public Brain(string name) : base(name)
    {
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, true)); 
        BodyLayers.Add(new OrganLayer(this));
    }

    public Brain(BodyPartBehaviour bodyPartBehaviour) : base(bodyPartBehaviour)
    {
        BodyLayers.Add(new CirculatoryLayer(this));
        BodyLayers.Add(new NerveLayer(this, true));
        BodyLayers.Add(new OrganLayer(this));
    }


    /// <summary>
    /// This takes all the pain in the body produced by body parts with nerves signal transmitters and sum it.
    /// </summary>
    /// <returns>a number between 0 and 1.</returns>
    public float ComputePain()
    {
        float pain = ComputePain(this);
        return pain;
    }

    private float ComputePain(BodyPart bodyPart)
    {
        float currentPain = 0;
        foreach (var part in bodyPart.ChildBodyParts)
        {
            currentPain += ComputePain(part);
        }

        var transmitters = bodyPart.NerveSignalTransmitters;
        foreach (var transmitter in transmitters)
        {
            if (transmitter.IsConnectedToCentralNervousSystem)
            {
                currentPain += transmitter.ProducePain();
            }
        }
        return currentPain;
    }
}
