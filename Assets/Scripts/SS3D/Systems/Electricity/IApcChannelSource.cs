namespace System.Electricity
{
    public interface IApcChannelSource : IElectricDevice
    {
        ApcControlFlags Channels { get; }
    }
}
