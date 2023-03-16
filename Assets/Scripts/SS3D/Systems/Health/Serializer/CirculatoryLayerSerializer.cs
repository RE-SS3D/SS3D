using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CirculatoryLayerSerializer
{
    public static void WriteCirculatoryLayer(this Writer writer, CirculatoryLayer layer)
    {
        BodyLayerSerializer.WriteDamages(writer, layer);
        BodyLayerSerializer.WriteSusceptibilities(writer, layer);
        BodyLayerSerializer.WriteResistances(writer, layer);

        writer.WriteNetworkBehaviour(layer.BodyPartBehaviour);
    }

    public static CirculatoryLayer ReadCirculatoryLayer(this Reader reader)
    {
        CirculatoryLayer layer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);


        var isCentralNervousSystem = reader.ReadBoolean();
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPartBehaviour = bodyPartGameObject.GetComponent<BodyPartBehaviour>();
        var bodyPart = bodyPartBehaviour.BodyPart;
        layer = new CirculatoryLayer(bodyPart, damages, susceptibilities, resistances);
        return layer;
    }
}

