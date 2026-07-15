namespace SS3D.Systems.Electricity
{
    public interface IApcChannelSource : IElectricDevice
    {
        ApcControlFlags Channels { get; }
    }
}
