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
    public float PainAmount { get; private set; }

    public override void Init(string name = "Brain")
    {
        base.Init(name);
        AddBrainLayer();
    }

    private void AddBrainLayer()
    {
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this, true));
        TryAddBodyLayer(new OrganLayer(this));
    }


    /// <summary>
    /// This takes all the pain in the body produced by body parts with nerves signal transmitters and sum it.
    /// </summary>
    /// <returns> a number between 0 and 1. The number can never be quite 1, since that would mean
    /// all layers took max damages including nerves (and in that case it would be zero pain).</returns>
    public float ComputeAveragePain()
    {
        int bodyPartCount = 0;
        float pain = ComputeAveragePain(this, ref bodyPartCount)/bodyPartCount;
        return pain;
    }

    private float ComputeAveragePain(BodyPart bodyPart, ref int bodyPartCount)
    {
        float currentPain = 0;
        foreach (var part in bodyPart.ChildBodyParts)
        {
            currentPain += ComputeAveragePain(part, ref bodyPartCount);
        }

        var transmitter = bodyPart.NerveSignalTransmitter;
        currentPain += transmitter.ProducePain();

        bodyPartCount++;
        return currentPain;
    }
}
