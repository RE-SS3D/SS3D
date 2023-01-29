using FishNet.Serializing;
using System;

public static class INerveSignalTransmitterSerializer
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
}
