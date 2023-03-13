using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Custom serializer for the nerve layer class.
/// </summary>
public static class NerveLayerSerializer 
{
    public static void WriteNerveLayer(this Writer writer, NerveLayer nerve)
    {
        // write INerveSignalTransmitter simple properties.
        writer.WriteBoolean(nerve.IsCentralNervousSystem);
        writer.WriteNetworkBehaviour(nerve.GetNetworkBehaviour);
    }

    public static NerveLayer ReadNerveLayer(this Reader reader)
    {
        NerveLayer nerveLayer;

        var isCentralNervousSystem = reader.ReadBoolean();
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPartBehaviour = bodyPartGameObject.GetComponent<BodyPartBehaviour>();
        var bodyPart = bodyPartBehaviour.BodyPart;
        nerveLayer = new NerveLayer(bodyPart, isCentralNervousSystem);
        return nerveLayer;
    }
}
