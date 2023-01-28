using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class NerveLayerSerializer 
{
    public static void WriteNerveLayer(this Writer writer, NerveLayer nerve)
    {
        // write INerveSignalTransmitter simple properties.
        writer.WriteBoolean(nerve.IsConnectedToCentralNervousSystem);
        writer.WriteGameObject(nerve.getGameObject);

        writer.WriteInt32(nerve.ParentConnectedSignalTransmitters().Count);
        foreach (var transmitter in nerve.ParentConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }

        writer.WriteInt32(nerve.ChildConnectedSignalTransmitters().Count);
        foreach (var transmitter in nerve.ChildConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }
    }

    public static NerveLayer ReadNerveLayer(this Reader reader)
    {
        NerveLayer nerveLayer;

        var isConnectedToCentralNervousSystem = reader.ReadBoolean();
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPartBehaviour = bodyPartGameObject.GetComponent<BodyPartBehaviour>();
        var bodyPart = bodyPartBehaviour.BodyPart;
        nerveLayer = new NerveLayer(bodyPart);

        var connectedParentsNumber = reader.ReadInt32();
        for (int i = 0; i < connectedParentsNumber; i++)
        {
            var transmitterGameObject = reader.ReadGameObject();
            nerveLayer.AddNerveSignalTransmitter(
                (INerveSignalTransmitter)transmitterGameObject.GetComponent<BodyPartBehaviour>().BodyPart.GetBodyLayer<INerveSignalTransmitter>(), true);
        }

        var connectedChildsNumber = reader.ReadInt32();
        for (int i = 0; i < connectedChildsNumber; i++)
        {
            var transmitterGameObject = reader.ReadGameObject();
            nerveLayer.AddNerveSignalTransmitter(
                (INerveSignalTransmitter)transmitterGameObject.GetComponent<BodyPartBehaviour>().BodyPart.GetBodyLayer<INerveSignalTransmitter>(), true);
        }

        return nerveLayer;
    }
}
