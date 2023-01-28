using FishNet.Serializing;
using System;

public static class INerveSignalTransmitterCustomSerializer
{

    public static void WriteINerveSignalTransmitter(this Writer writer, INerveSignalTransmitter value)
    {
        writer.WriteInt32((int)value.TransmitterId);
        if(value.TransmitterId == NerveSignalTransmitterEnum.Nerve)
        {
            writer.WriteNerveLayer((NerveLayer)value);
        }
    }

    public static INerveSignalTransmitter ReadINerveSignalTransmitter(this Reader reader)
    {
        var transmitterId = (NerveSignalTransmitterEnum)reader.ReadInt32();
        if (transmitterId == NerveSignalTransmitterEnum.Nerve)
        {
            return reader.ReadNerveLayer();
        }
        else
        {
            throw new ArgumentException();
        }
    }

    /*
    private static INerveSignalTransmitter WriteConnections(this Writer writer, INerveSignalTransmitter value)
    {
        writer.WriteInt32(value.ParentConnectedSignalTransmitters().Count);
        foreach (var transmitter in value.ParentConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }

        writer.WriteInt32(value.ChildConnectedSignalTransmitters().Count);
        foreach (var transmitter in value.ChildConnectedSignalTransmitters())
        {
            writer.WriteGameObject(transmitter.getGameObject);
        }



        return value;
    }

    
    private static NerveLayer ReadNerveLayer(this Reader reader)
    {
        var bodyPart = reader.ReadGameObject().GetComponent<BodyPartBehaviour>().BodyPart;
        var nerveSignalTransmitter = new NerveLayer(bodyPart);
        return nerveSignalTransmitter;
    }

    private static INerveSignalTransmitter ReadConnections(this Reader reader, INerveSignalTransmitter nerveSignalTransmitter)
    {
        var connectedParentsNumber = reader.ReadInt32();
        for (int i = 0; i < connectedParentsNumber; i++)
        {
            var transmitterGameObject = reader.ReadGameObject();
            nerveSignalTransmitter.AddNerveSignalTransmitter(
                (INerveSignalTransmitter)transmitterGameObject.GetComponent<BodyPartBehaviour>().BodyPart.GetBodyLayer<INerveSignalTransmitter>(), true);
        }

        var connectedChildsNumber = reader.ReadInt32();
        for (int i = 0; i < connectedChildsNumber; i++)
        {
            var transmitterGameObject = reader.ReadGameObject();
            nerveSignalTransmitter.AddNerveSignalTransmitter(
                (INerveSignalTransmitter)transmitterGameObject.GetComponent<BodyPartBehaviour>().BodyPart.GetBodyLayer<INerveSignalTransmitter>(), true);
        }

        return nerveSignalTransmitter;
    }
    */
}
