using FishNet.Serializing;
using System;

public static class INerveSignalTransmitterSerializer
{

    public static void WriteINerveSignalTransmitter(this Writer writer, INerveSignalTransmitter value)
    {
        writer.WriteInt32((int)value.TransmitterId);
        if(value.TransmitterId == NerveSignalTransmitterType.Nerve)
        {
            writer.WriteNerveLayer((NerveLayer)value);
        }
    }

    public static INerveSignalTransmitter ReadINerveSignalTransmitter(this Reader reader)
    {
        var transmitterId = (NerveSignalTransmitterType)reader.ReadInt32();
        if (transmitterId == NerveSignalTransmitterType.Nerve)
        {
            return reader.ReadNerveLayer();
        }
        else
        {
            throw new ArgumentException();
        }
    }
}
