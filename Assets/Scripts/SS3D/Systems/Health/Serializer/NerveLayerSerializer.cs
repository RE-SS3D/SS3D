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
        BodyLayerSerializer.WriteDamages(writer, nerve);
        BodyLayerSerializer.WriteSusceptibilities(writer, nerve);
        BodyLayerSerializer.WriteResistances(writer, nerve);

        writer.WriteNetworkBehaviour(nerve.BodyPart);
    }

    public static NerveLayer ReadNerveLayer(this Reader reader)
    {
        NerveLayer nerveLayer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);
  
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPart = bodyPartGameObject.GetComponent<BodyPart>();


        nerveLayer = new NerveLayer(bodyPart, damages, susceptibilities, resistances);
        return nerveLayer;
    }
}
