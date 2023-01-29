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
        writer.WriteBoolean(nerve.IsConnectedToCentralNervousSystem);
        writer.WriteNetworkBehaviour(nerve.GetNetworkBehaviour);
        ConnectionsWrite(writer, nerve);
    }

    public static NerveLayer ReadNerveLayer(this Reader reader)
    {
        NerveLayer nerveLayer;

        var isConnectedToCentralNervousSystem = reader.ReadBoolean();
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPartBehaviour = bodyPartGameObject.GetComponent<BodyPartBehaviour>();
        var bodyPart = bodyPartBehaviour.BodyPart;
        nerveLayer = new NerveLayer(bodyPart);

        nerveLayer = ConnectionsRead(reader, nerveLayer);

        return nerveLayer;
    }

    private static NerveLayer ConnectionsRead(this Reader reader, NerveLayer nerveLayer)
    {
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

    private static void ConnectionsWrite(this Writer writer, NerveLayer nerveLayer)
    {
        writer.WriteInt32(nerveLayer.ParentConnectedSignalTransmitters().Count);
        foreach (var transmitter in nerveLayer.ParentConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }

        writer.WriteInt32(nerveLayer.ChildConnectedSignalTransmitters().Count);
        foreach (var transmitter in nerveLayer.ChildConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }
    }


}
